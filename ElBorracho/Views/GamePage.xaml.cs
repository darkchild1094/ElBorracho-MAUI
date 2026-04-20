using ElBorracho.Models;
using ElBorracho.ViewModels;

namespace ElBorracho.Views;

public partial class GamePage : ContentPage
{
    private readonly GameViewModel _vm;
    private bool _sidebarOpen = false;
    private double _panStartX, _panStartY;

    private static readonly long[]   SpeedLevels = GameViewModel.SpeedLevels;
    private static readonly string[] SpeedLabels = GameViewModel.SpeedLabels;

    // Original ghost card positions
    private static readonly double[] GhostRotations   = { 4, -6, 9, -11 };
    private static readonly double[] GhostTranslatesX = { 3, -4, 6, -8 };
    private static readonly double[] GhostTranslatesY = { 1, 2, 4, 6 };

    private const int TotalCards = 54;

    public GamePage(GameViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;

        _vm.SpeakRequested           += OnSpeakRequested;
        _vm.PropertyChanged          += OnViewModelPropertyChanged;
        _vm.ShuffleAnimationRequested += async () =>
            await MainThread.InvokeOnMainThreadAsync(AnimateShuffleAsync);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    protected override bool OnBackButtonPressed()
    {
        if (_sidebarOpen) { _ = CloseSidebarAsync(); return true; }
        return base.OnBackButtonPressed();
    }

    // ── ViewModel observer ────────────────────────────────────────────────────
    private void OnViewModelPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (e.PropertyName == nameof(GameViewModel.CurrentCard))
            {
                AnimateCardIn();
                ScrollHistoryToEnd();
                UpdateProgressBar();
            }
            if (e.PropertyName == nameof(GameViewModel.GameState))
            {
                UpdatePlayButtonColor();
                if (_vm.GameState == GameState.Finished)
                    ShowToast("¡Fin de la baraja! 🎴 54/54 cartas");
            }
        });
    }

    // ── TTS ───────────────────────────────────────────────────────────────────
    private async void OnSpeakRequested(string text)
    {
        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            var spanish = locales.FirstOrDefault(l =>
                l.Language.StartsWith("es", StringComparison.OrdinalIgnoreCase));
            await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions
            {
                Locale = spanish,
                Volume = 1.0f,
                Pitch  = 1.0f
            });
        }
        catch { /* TTS not available on this device — fail silently */ }
    }

    // ── Progress bar ──────────────────────────────────────────────────────────
    private void UpdateProgressBar()
    {
        int played = TotalCards - _vm.RemainingCount;
        double progress = (double)played / TotalCards;
        _ = ProgressBar.ProgressTo(progress, 300, Easing.CubicOut);
    }

    // ── Play button color ─────────────────────────────────────────────────────
    private void UpdatePlayButtonColor()
    {
        BtnPlay.BackgroundColor = _vm.GameState switch
        {
            GameState.Playing  => Color.FromArgb("#C4940C"), // amber = running
            GameState.Finished => Color.FromArgb("#3D1D2A"), // dark  = done
            _                  => Color.FromArgb("#D4175A")  // red   = start/resume
        };
    }

    // ── Sidebar ───────────────────────────────────────────────────────────────
    private void OnMenuClicked(object? sender, EventArgs e)
        => _ = OpenSidebarAsync();

    private void OnOverlayTapped(object? sender, TappedEventArgs e)
        => _ = CloseSidebarAsync();

    private void OnCloseSidebar(object? sender, EventArgs e)
        => _ = CloseSidebarAsync();

    private async Task OpenSidebarAsync()
    {
        if (_sidebarOpen) return;
        _sidebarOpen = true;
        SidebarOverlay.IsVisible = true;
        await Task.WhenAll(
            SidebarOverlay.FadeToAsync(1, 200),
            SidebarPanel.TranslateToAsync(0, 0, 280, Easing.CubicOut));
    }

    private async Task CloseSidebarAsync()
    {
        if (!_sidebarOpen) return;
        _sidebarOpen = false;
        await Task.WhenAll(
            SidebarOverlay.FadeToAsync(0, 200),
            SidebarPanel.TranslateToAsync(-300, 0, 250, Easing.CubicIn));
        SidebarOverlay.IsVisible = false;
    }

    // ── Sidebar actions ───────────────────────────────────────────────────────
    private async void OnGeneradorClicked(object? sender, EventArgs e)
    {
        await CloseSidebarAsync();
        await Shell.Current.GoToAsync("generador");
    }

    private async void OnBugClicked(object? sender, EventArgs e)
    {
        await CloseSidebarAsync();
        try
        {
            await Email.Default.ComposeAsync(new EmailMessage
            {
                Subject = "Bug report — Lotería El Borracho",
                To      = new List<string> { "soporte@loteriaelborracho.mx" }
            });
        }
        catch { ShowToast("No se encontró app de correo"); }
    }

    private void OnSalirClicked(object? sender, EventArgs e)
    {
        try
        {
#if ANDROID
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#else
            Application.Current?.Quit();
#endif
        }
        catch { Application.Current?.Quit(); }
    }

    // ── Speed ─────────────────────────────────────────────────────────────────
    private void OnSpeedChanged(object? sender, ValueChangedEventArgs e)
    {
        int idx = Math.Clamp((int)Math.Round(e.NewValue), 0, SpeedLevels.Length - 1);
        LblSpeedName.Text  = SpeedLabels[idx];
        LblSpeedValue.Text = $"{SpeedLevels[idx] / 1000} s";
        _vm.ApplySpeedIndex(idx);
    }

    // ── Card tap ──────────────────────────────────────────────────────────────
    private void OnCardTapped(object? sender, TappedEventArgs e)
        => _vm.ToggleAutoPlay();

    // ── Intro card pan/drag ───────────────────────────────────────────────────
    private void OnIntroPan(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panStartX = IntroCard.TranslationX;
                _panStartY = IntroCard.TranslationY;
                break;
            case GestureStatus.Running:
                IntroCard.TranslationX = _panStartX + e.TotalX;
                IntroCard.TranslationY = _panStartY + e.TotalY;
                IntroCard.Rotation     = -2 + (e.TotalX * 0.03);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _ = Task.WhenAll(
                    IntroCard.TranslateToAsync(0, 0, 300, Easing.SpringOut),
                    IntroCard.RotateToAsync(-2, 300, Easing.CubicOut));
                break;
        }
    }

    // ── Shuffle animation ─────────────────────────────────────────────────────
    private Border[] Ghosts => new[] { Ghost2, Ghost3, Ghost4, Ghost5 };

    private async Task AnimateShuffleAsync()
    {
        var rng    = new Random();
        var ghosts = Ghosts;

        for (int pass = 0; pass < 3; pass++)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < ghosts.Length; i++)
            {
                var g  = ghosts[i];
                var dx = rng.Next(-35, 35);
                var dy = rng.Next(-25, 25);
                var dr = rng.Next(-30, 30);
                tasks.Add(Task.WhenAll(
                    g.TranslateToAsync(GhostTranslatesX[i] + dx,
                                       GhostTranslatesY[i] + dy, 130, Easing.CubicOut),
                    g.RotateToAsync(GhostRotations[i] + dr, 130, Easing.CubicOut),
                    g.FadeToAsync(0.7, 130)));
            }
            tasks.Add(IntroCard.RotateToAsync(-2 + rng.Next(-10, 10), 130, Easing.CubicOut));
            await Task.WhenAll(tasks);
            await Task.Delay(55);

            tasks.Clear();
            for (int i = 0; i < ghosts.Length; i++)
            {
                var g    = ghosts[i];
                var newX = GhostTranslatesX[i] + rng.Next(-5, 5);
                var newY = GhostTranslatesY[i] + rng.Next(-4, 4);
                var newR = GhostRotations[i]   + rng.Next(-4, 4);
                tasks.Add(Task.WhenAll(
                    g.TranslateToAsync(newX, newY, 150, Easing.CubicIn),
                    g.RotateToAsync(newR, 150, Easing.CubicIn),
                    g.FadeToAsync(0.3, 150)));
            }
            tasks.Add(IntroCard.RotateToAsync(-2, 150, Easing.CubicIn));
            await Task.WhenAll(tasks);
            await Task.Delay(45);
        }

        ShowToast("¡Baraja mezclada! 🔀");
    }

    // ── Card entrance animation ───────────────────────────────────────────────
    private void AnimateCardIn()
    {
        if (_vm.CurrentCard == null) return;
        CardFrame.Opacity      = 0;
        CardFrame.Scale        = 0.92;
        CardFrame.TranslationY = 20;
        _ = Task.WhenAll(
            CardFrame.FadeToAsync(1, 240),
            CardFrame.ScaleToAsync(1, 240, Easing.CubicOut),
            CardFrame.TranslateToAsync(0, 0, 240, Easing.CubicOut));
    }

    // ── History scroll ────────────────────────────────────────────────────────
    private void ScrollHistoryToEnd()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(60);
            await HistScroll.ScrollToAsync(double.MaxValue, 0, false);
        });
    }

    // ── Toast ─────────────────────────────────────────────────────────────────
    private async void ShowToast(string message)
    {
        try
        {
            var toast = CommunityToolkit.Maui.Alerts.Toast.Make(
                message,
                CommunityToolkit.Maui.Core.ToastDuration.Short, 14);
            await toast.Show(CancellationToken.None);
        }
        catch { /* Toast not supported — fail silently */ }
    }
}
