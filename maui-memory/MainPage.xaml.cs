using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace maui_memory;

public partial class MainPage : ContentPage
{
    private const int ExpectedCols = 2;
    private const int ExpectedRows = 5;
    private readonly ReadOnlyDictionary<string, string> _eng_kat = new Dictionary<string, string>()
    {
        { "a", "ア" },
        { "e", "エ" },
        { "i", "イ" },
        { "o", "オ" },
        { "u", "ウ" },
        { "ka", "カ" },
        { "ke", "ケ" },
        { "ki", "キ" },
        { "ko", "コ" },
        { "ku", "ク" },
        { "sa", "サ" },
        { "se", "セ" },
        { "shi", "シ" },
        { "so", "ソ" },
        { "su", "ス" },
        { "ta", "タ" },
        { "te", "テ" },
        { "chi", "チ" },
        { "to", "ト" },
        { "tsu", "ツ" },
        { "na", "ナ" },
        { "ne", "ネ" },
        { "ni", "ニ" },
        { "no", "ノ" },
        { "nu", "ヌ" },
        { "ha", "ハ" },
        { "he", "ヘ" },
        { "hi", "ヒ" },
        { "ho", "ホ" },
        { "fu", "フ" },
        { "ma", "マ" },
        { "me", "メ" },
        { "mi", "ミ" },
        { "mo", "モ" },
        { "mu", "ム" },
        { "ya", "ヤ" },
        { "yu", "ユ" },
        { "yo", "ヨ" },
        { "ra", "ラ" },
        { "re", "レ" },
        { "ri", "リ" },
        { "ro", "ロ" },
        { "ru", "ル" },
        { "wa", "ワ" },
        { "wo", "ヲ" },
        { "n", "ン" },
        { "kya", "キャ" },
        { "kyu", "キュ" },
        { "kyo", "キョ" },
        { "sha", "シャ" },
        { "shu", "シュ" },
        { "sho", "ショ" },
        { "cha", "チャ" },
        { "chu", "チュ" },
        { "cho", "チョ" },
        { "nya", "ニャ" },
        { "nyu", "ニュ" },
        { "nyo", "ニョ" },
        { "hya", "ヒャ" },
        { "hyu", "ヒュ" },
        { "hyo", "ヒョ" },
        { "mya", "ミャ" },
        { "myu", "ミュ" },
        { "myo", "ミョ" },
        { "rya", "リャ" },
        { "ryu", "リュ" },
        { "ryo", "リョ" },
    }.AsReadOnly();
    private Button? _leftButton = null;
    private Button? _rightButton = null;
    private readonly Random _random;
    private ConcurrentDictionary<string, string> _currentSymbols = new();
    private readonly Button[][] _buttons;
    private uint _rightAnswers = 0;
    private uint _wrongAnswers = 0;

    public MainPage()
    {
        InitializeComponent();

        Debug.Assert(TableGrid.ColumnDefinitions.Count == ExpectedCols);
        Debug.Assert(TableGrid.RowDefinitions.Count == ExpectedRows);

        _random = new Random();
        _buttons = [
            [Button00, Button01],
            [Button10, Button11],
            [Button20, Button21],
            [Button30, Button31],
            [Button40, Button41],
        ];
    }

    private void OnStartButtonClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button != StartStopButton) return;

        StartStopButton.IsEnabled = false;
        StartStopButton.Text = "Stop";
        StartStopButton.Clicked -= OnStartButtonClicked!;
        StartStopButton.Clicked += OnStopButtonClicked!;
        StartStopButton.IsEnabled = true;

        _rightAnswers = 0;
        _wrongAnswers = 0;
        UpdateCountsLabel();

        TableGrid.IsVisible = true;

        _currentSymbols = new(_eng_kat);

        // Initial fill of the cells, which will remove the first 5 kvps from _currentSymbols.
        for (int i = 0; i < _buttons.Length; i++)
        {
            (string key, string value) = _currentSymbols.Pop(_random.Next(_currentSymbols.Count));
            Debug.Assert(!_currentSymbols.ContainsKey(key));
            _buttons[i][0].Text = key;
            _buttons[i][1].Text = value;
        }

        RandomizeButtonTexts();
    }

    private void OnStopButtonClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button != StartStopButton) return;

        StartStopButton.IsEnabled = false;
        StartStopButton.Text = "Start";
        StartStopButton.Clicked -= OnStopButtonClicked!;
        StartStopButton.Clicked += OnStartButtonClicked!;
        StartStopButton.IsEnabled = true;

        LabelMessages.Text = "";
        LabelCounts.Text = "";

        TableGrid.IsVisible = false;

        foreach (Button[] row in _buttons)
        {
            row[0].IsEnabled = true;
            row[1].IsEnabled = true;
            MarkUnselectedButton(row[0]);
            MarkUnselectedButton(row[1]);
        }
        
        _currentSymbols.Clear();
    }

    private async void OnButtonClickedAsync(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            if (!button.IsEnabled)
            {
                return;
            }

            // If a left button is clicked
            if (button == Button00 || button == Button10 || button == Button20 || button == Button30 || button == Button40)
            {
                // There was already a selected left button before
                if (_leftButton != null)
                {
                    // A different left button was picked, reset the old one, mark the new one as selected
                    if (_leftButton != button)
                    {
                        MarkUnselectedButton(_leftButton);
                        ChangeButtonColor(button, Colors.Cyan);
                        _leftButton = button;
                    }
                    // The same button was clicked, reset it
                    else
                    {
                        MarkUnselectedButton(button);
                        _leftButton = null;
                    }
                }
                // A fresh new left button is being selected
                else
                {
                    ChangeButtonColor(button, Colors.Cyan);
                    _leftButton = button;
                }

                // If a left was selected (not reset), check if there was already a selected right button and see if they match
                await VerifyMatchAsync();
            }
            // If a right button is clicked
            else if (button == Button01 || button == Button11 || button == Button21 || button == Button31 || button == Button41)
            {
                // There was already a selected right button before
                if (_rightButton != null)
                {
                    // A different right button was picked, reset the old one, mark the new one as selected
                    if (_rightButton != button)
                    {
                        MarkUnselectedButton(_rightButton);
                        ChangeButtonColor(button, Colors.Cyan);
                        _rightButton = button;
                    }
                    // The same button was clicked, reset it
                    else
                    {
                        MarkUnselectedButton(button);
                        _rightButton = null;
                    }
                }
                // A fresh new right button is being selected
                else
                {
                    ChangeButtonColor(button, Colors.Cyan);
                    _rightButton = button;
                }

                // If a right was selected (not reset), check if there was already a selected left button and see if they match
                await VerifyMatchAsync();
            }
        }
    }

    private async Task VerifyMatchAsync()
    {
        if (_leftButton != null && _rightButton != null)
        {
            Debug.Assert(_leftButton.BindingContext is Color);
            Debug.Assert(_rightButton.BindingContext is Color);
            // Confirm if they both match
            if (_eng_kat.TryGetValue(_leftButton.Text, out string? currentValue) && currentValue == _rightButton.Text)
            {
                _rightAnswers++;
                // Flash both buttons green
                await FlashButtonGreenAsync(_leftButton);
                await FlashButtonGreenAsync(_rightButton);
                if (_currentSymbols.Any())
                {
                    // Replace both button texts with new ones
                    (string newKey, string newValue) = _currentSymbols.Pop(_random.Next(0, _currentSymbols.Count));
                    Debug.Assert(!_currentSymbols.ContainsKey(newKey));
                    _leftButton.Text = newKey;
                    _rightButton.Text = newValue;

                    RandomizeButtonTexts();
                }
                else
                {
                    _leftButton.Text = string.Empty;
                    _rightButton.Text = string.Empty;
                    _leftButton.IsEnabled = false;
                    _rightButton.IsEnabled = false;
                    ChangeButtonColor(_leftButton, Colors.DimGray);
                    ChangeButtonColor(_rightButton, Colors.DimGray);
                }
            }
            // They didn't match
            else
            {
                _wrongAnswers++;

                // Flash both buttons red
                await FlashButtonRedAsync(_leftButton);
                await FlashButtonRedAsync(_rightButton);
                // Reset both
                MarkUnselectedButton(_leftButton);
                MarkUnselectedButton(_rightButton);
            }

            // Finally, clear both, regardless of outcome
            _leftButton = null;
            _rightButton = null;

            UpdateCountsLabel();

            // Final check: if all buttons are disabled, the game is done
            if (!_buttons.Any(row => row[0].IsEnabled || row[1].IsEnabled))
            {
                LabelMessages.Text = "You won!";
            }
        }
    }

    private void UpdateCountsLabel()
    {
        LabelCounts.Text = $"Right: {_rightAnswers}, Wrong: {_wrongAnswers}";
    }

    private void RandomizeButtonTexts()
    {
        var leftTexts = _buttons.Select(row => row[0].Text).OrderBy(_ => _random.Next()).ToList();
        var rightTexts = _buttons.Select(row => row[1].Text).OrderBy(_ => _random.Next()).ToList();
        
        Debug.Assert(leftTexts.Count() == _buttons.Length);
        Debug.Assert(rightTexts.Count() == _buttons.Length);
        
        for (int i = 0; i < _buttons.Length; i++)
        {
            _buttons[i][0].Text = leftTexts.ElementAt(i);
            _buttons[i][1].Text = rightTexts.ElementAt(i);
        }
    }

    private Task FlashButtonGreenAsync(Button button) => FlashButtonColorAsync(button, Colors.Green);
    private Task FlashButtonRedAsync(Button button) => FlashButtonColorAsync(button, Colors.Red);

    private void MarkUnselectedButton(Button? button)
    {
        if (button?.BindingContext is Color originalColor)
        {
            button.BackgroundColor = originalColor;
        }
    }

    private async Task FlashButtonColorAsync(Button button, Color color)
    {
        ChangeButtonColor(button, color);
        await Task.Delay(500);
        button.BackgroundColor = (Color)button.BindingContext;
    }
    
    private void ChangeButtonColor(Button button, Color color)
    {
        if (button.BindingContext is not Color)
        {
            Color originalColor = button.BackgroundColor;
            button.BindingContext = originalColor;
        }
        button.BackgroundColor = color;
    }

    private IEnumerable<int> GetRandomNumberSequence(int count)
    {
        List<int> numbers = Enumerable.Range(0, count).ToList();
        for (int c = 0; c < count; c++)
        {
            int pos = _random.Next(0, numbers.Count);
            int number = numbers.ElementAt(pos);
            numbers.RemoveAt(pos);
            yield return number;
        }
    }
}

