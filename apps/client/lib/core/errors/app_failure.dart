enum AppFailureKind {
  network,
  timeout,
  unauthenticated,
  forbidden,
  notFound,
  validation,
  invalidPayload,
  upload,
  cancelled,
  unknown,
}

class AppFailure implements Exception {
  const AppFailure(this.kind, {this.statusCode, this.stackTrace});

  final AppFailureKind kind;
  final int? statusCode;
  final StackTrace? stackTrace;

  @override
  String toString() => 'AppFailure(${kind.name}, status: $statusCode)';
}
