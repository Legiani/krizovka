typedef CancellationListener = void Function();

/// Cancellation primitive shared by application/domain code without depending
/// on a networking package or a Flutter lifecycle type.
class CancellationSignal {
  bool _isCancelled = false;
  final Set<CancellationListener> _listeners = {};

  bool get isCancelled => _isCancelled;

  void Function() addListener(CancellationListener listener) {
    if (_isCancelled) {
      listener();
      return () {};
    }
    _listeners.add(listener);
    return () => _listeners.remove(listener);
  }

  void _cancel() {
    if (_isCancelled) return;
    _isCancelled = true;
    final listeners = List<CancellationListener>.of(_listeners);
    _listeners.clear();
    for (final listener in listeners) {
      listener();
    }
  }
}

class CancellationSource {
  final CancellationSignal signal = CancellationSignal();

  void cancel() => signal._cancel();
}
