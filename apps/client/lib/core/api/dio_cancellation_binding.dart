import 'package:dio/dio.dart';
import 'package:krizovka_client/core/errors/cancellation.dart';

/// Bridges the package-neutral application cancellation signal to Dio.
class DioCancellationBinding {
  DioCancellationBinding(CancellationSignal? signal) : _signal = signal {
    if (signal != null) {
      _removeListener = signal.addListener(() {
        if (!token.isCancelled) token.cancel('Operation cancelled');
      });
    }
  }

  final CancellationSignal? _signal;
  final CancelToken token = CancelToken();
  void Function()? _removeListener;

  CancelToken? get cancelToken => _signal == null ? null : token;

  void dispose() {
    _removeListener?.call();
    _removeListener = null;
  }
}
