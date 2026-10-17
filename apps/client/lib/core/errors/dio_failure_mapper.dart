import 'package:dio/dio.dart';
import 'package:krizovka_client/core/errors/app_failure.dart';

/// Maps transport-specific exceptions at the Dio/data boundary.
AppFailure mapDioFailure(DioException error, StackTrace stackTrace) {
  final status = error.response?.statusCode;
  final kind = switch (status) {
    401 => AppFailureKind.unauthenticated,
    403 => AppFailureKind.forbidden,
    404 => AppFailureKind.notFound,
    _ => switch (error.type) {
      DioExceptionType.connectionTimeout ||
      DioExceptionType.sendTimeout ||
      DioExceptionType.receiveTimeout => AppFailureKind.timeout,
      DioExceptionType.cancel => AppFailureKind.cancelled,
      DioExceptionType.connectionError => AppFailureKind.network,
      _ => AppFailureKind.unknown,
    },
  };
  return AppFailure(kind, statusCode: status, stackTrace: stackTrace);
}
