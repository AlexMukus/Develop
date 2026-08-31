using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Infrastructure.Analysis;

/// <summary>
/// Сервис тестирования на ghosting / NKRO.
/// </summary>
public sealed class GhostingTestEngine : IGhostingTestEngine, IDisposable
{
    private readonly IRawInputCapture _rawInputCapture;
    private readonly ILayoutProvider _layoutProvider;
    private readonly Dictionary<uint, PhysicalKey> _scanCodeToKey;
    private readonly HashSet<PhysicalKey> _currentlyPressed = new();
    private readonly List<GhostingTestResult> _results = new();
    private readonly object _lock = new();

    private bool _isRunning;
    private int _maxSimultaneousKeys;

    /// <inheritdoc />
    public event EventHandler<GhostingTestResult>? TestResultUpdated;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public IReadOnlyList<PhysicalKey> CurrentlyPressedKeys
    {
        get
        {
            lock (_lock)
            {
                return _currentlyPressed.ToList();
            }
        }
    }

    /// <summary>
    /// Создаёт экземпляр <see cref="GhostingTestEngine"/>.
    /// </summary>
    public GhostingTestEngine(IRawInputCapture rawInputCapture, ILayoutProvider layoutProvider)
    {
        _rawInputCapture = rawInputCapture ?? throw new ArgumentNullException(nameof(rawInputCapture));
        _layoutProvider = layoutProvider ?? throw new ArgumentNullException(nameof(layoutProvider));

        _scanCodeToKey = BuildScanCodeMap();

        _rawInputCapture.KeyPressed += OnKeyPressed;
        _rawInputCapture.KeyReleased += OnKeyReleased;
    }

    /// <inheritdoc />
    public void StartTest()
    {
        lock (_lock)
        {
            _isRunning = true;
            _currentlyPressed.Clear();
            _results.Clear();
            _maxSimultaneousKeys = 0;
        }
    }

    /// <inheritdoc />
    public void StopTest()
    {
        _isRunning = false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_lock)
        {
            _currentlyPressed.Clear();
            _results.Clear();
            _maxSimultaneousKeys = 0;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _rawInputCapture.KeyPressed -= OnKeyPressed;
        _rawInputCapture.KeyReleased -= OnKeyReleased;
    }

    private void OnKeyPressed(object? sender, RawKeyEventArgs e)
    {
        if (!_isRunning)
        {
            return;
        }

        PhysicalKey? key = FindKey(e.ScanCode);
        if (key == null)
        {
            return;
        }

        GhostingTestResult result;

        lock (_lock)
        {
            _currentlyPressed.Add(key);

            if (_currentlyPressed.Count > _maxSimultaneousKeys)
            {
                _maxSimultaneousKeys = _currentlyPressed.Count;
            }

            IReadOnlyList<PhysicalKey> pressed = _currentlyPressed.ToList();
            result = new GhostingTestResult(
                DateTime.Now,
                pressed,
                pressed,
                pressed.Count > 6,
                pressed.Count);

            _results.Add(result);
        }

        TestResultUpdated?.Invoke(this, result);
    }

    private void OnKeyReleased(object? sender, RawKeyEventArgs e)
    {
        PhysicalKey? key = FindKey(e.ScanCode);
        if (key == null)
        {
            return;
        }

        lock (_lock)
        {
            _currentlyPressed.Remove(key);
        }
    }

    private PhysicalKey? FindKey(uint scanCode)
    {
        if (_scanCodeToKey.TryGetValue(scanCode, out PhysicalKey? key))
        {
            return key;
        }

        return null;
    }

    private Dictionary<uint, PhysicalKey> BuildScanCodeMap()
    {
        var map = new Dictionary<uint, PhysicalKey>();

        foreach (KeyboardLayout layout in _layoutProvider.SupportedLayouts)
        {
            foreach (PhysicalKey key in _layoutProvider.GetKeys(layout))
            {
                map[key.ScanCode] = key;
            }
        }

        return map;
    }
}
