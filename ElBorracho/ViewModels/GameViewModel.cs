using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElBorracho.Models;
using ElBorracho.Services;

namespace ElBorracho.ViewModels;

public partial class GameViewModel : ObservableObject
{
    // ─── Constantes ───────────────────────────────────────────────────────────
    public const long DefaultIntervalMs = 5_000L;
    public const long MinIntervalMs = 1_000L;
    public const long MaxIntervalMs = 15_000L;
    public const string IntroPhrase = "Corre y se va con";

    public static readonly long[] SpeedLevels = { 1000, 2500, 5000, 8000, 12000, 15000 };
    public static readonly string[] SpeedLabels = { "Relámpago ⚡", "Rápida", "Normal", "Tranquila", "De fiesta 🎉", "Tortuga 🐢" };

    private readonly CartaRepository _repository;
    private List<Carta> _deck = new();
    private int _currentIndex = 0;
    private CancellationTokenSource? _autoPlayCts;

    // ─── Propiedades observables ──────────────────────────────────────────────
    [ObservableProperty] private GameState _gameState = GameState.Idle;
    [ObservableProperty] private Carta? _currentCard;
    [ObservableProperty] private List<Carta> _playedCards = new();
    [ObservableProperty] private int _remainingCount;
    [ObservableProperty] private long _cardIntervalMs = DefaultIntervalMs;
    [ObservableProperty] private double _speedIndex = 2;

    // Eventos hacia la vista
    public event Action<string>? SpeakRequested;
    public event Action? ShuffleAnimationRequested;

    // ─── Propiedades derivadas ────────────────────────────────────────────────
    public string PlayButtonText => GameState switch
    {
        GameState.Playing => "Pausar",
        GameState.Paused => "Reanudar",
        _ => "Iniciar"
    };

    public bool CanShuffle => GameState == GameState.Idle;
    public bool CanNext => GameState == GameState.Idle || GameState == GameState.Paused;
    public bool CanPlayPause => GameState != GameState.Finished;
    public bool CanReset => GameState != GameState.Playing;

    // ─── Constructor ──────────────────────────────────────────────────────────
    public GameViewModel(CartaRepository repository)
    {
        _repository = repository;
        ResetGame();
    }

    // ─── Comandos ─────────────────────────────────────────────────────────────
    [RelayCommand]
    public void ToggleAutoPlay()
    {
        switch (GameState)
        {
            case GameState.Idle:
                SpeakRequested?.Invoke(IntroPhrase);
                _ = StartAutoPlayAsync();
                break;
            case GameState.Paused:
                _ = StartAutoPlayAsync();
                break;
            case GameState.Playing:
                PauseAutoPlay();
                break;
        }
    }

    [RelayCommand]
    public void NextCardManual()
    {
        if (GameState == GameState.Idle || GameState == GameState.Paused)
            AdvanceCard();
    }

    [RelayCommand]
    public void ShuffleDeck()
    {
        if (GameState == GameState.Idle)
        {
            Shuffle(_deck);
            ShuffleAnimationRequested?.Invoke();
            RefreshDerivedProps();
        }
    }

    [RelayCommand]
    public void ResetGame()
    {
        _autoPlayCts?.Cancel();
        _autoPlayCts = null;
        _deck = _repository.FreshDeck();
        Shuffle(_deck);
        _currentIndex = 0;
        CurrentCard = null;
        PlayedCards = new List<Carta>();
        RemainingCount = _deck.Count;
        GameState = GameState.Idle;
        ShuffleAnimationRequested?.Invoke();
        RefreshDerivedProps();
    }

    public void ApplySpeedIndex(int idx)
    {
        SpeedIndex = idx;
        CardIntervalMs = Math.Clamp(SpeedLevels[idx], MinIntervalMs, MaxIntervalMs);
        if (GameState == GameState.Playing)
        {
            _autoPlayCts?.Cancel();
            _ = StartAutoPlayAsync();
        }
    }

    // ─── Lógica interna ───────────────────────────────────────────────────────
    private async Task StartAutoPlayAsync()
    {
        _autoPlayCts?.Cancel();
        _autoPlayCts = new CancellationTokenSource();
        var token = _autoPlayCts.Token;

        GameState = GameState.Playing;
        RefreshDerivedProps();

        try
        {
            while (!token.IsCancellationRequested && _currentIndex < _deck.Count)
            {
                await Task.Delay((int)CardIntervalMs, token);
                if (!token.IsCancellationRequested)
                    AdvanceCard();
            }
            if (!token.IsCancellationRequested)
            {
                GameState = GameState.Finished;
                RefreshDerivedProps();
            }
        }
        catch (OperationCanceledException) { }
    }

    private void PauseAutoPlay()
    {
        _autoPlayCts?.Cancel();
        _autoPlayCts = null;
        GameState = GameState.Paused;
        RefreshDerivedProps();
    }

    private void AdvanceCard()
    {
        if (_currentIndex >= _deck.Count) return;
        var carta = _deck[_currentIndex++];
        CurrentCard = carta;
        PlayedCards = new List<Carta>(PlayedCards) { carta };
        RemainingCount = _deck.Count - _currentIndex;
        SpeakRequested?.Invoke(carta.Nombre);
        RefreshDerivedProps();
    }

    private void RefreshDerivedProps()
    {
        OnPropertyChanged(nameof(PlayButtonText));
        OnPropertyChanged(nameof(CanShuffle));
        OnPropertyChanged(nameof(CanNext));
        OnPropertyChanged(nameof(CanPlayPause));
        OnPropertyChanged(nameof(CanReset));
    }

    private static void Shuffle<T>(List<T> list)
    {
        var rng = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}