import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:krizovka_client/features/reports/models/report_models.dart';

class ApiBaseUrlNotifier extends Notifier<String> {
  @override
  String build() => 'http://127.0.0.1:5080';

  void setUrl(String url) => state = url;
}

final apiBaseUrlProvider =
    NotifierProvider<ApiBaseUrlNotifier, String>(ApiBaseUrlNotifier.new);

final dioProvider = Provider<Dio>((ref) {
  final baseUrl = ref.watch(apiBaseUrlProvider);
  return Dio(
    BaseOptions(
      baseUrl: baseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 10),
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
      },
    ),
  );
});

final reportsApiClientProvider = Provider<ReportsApiClient>((ref) {
  final dio = ref.watch(dioProvider);
  return ReportsApiClient(dio);
});

class ReportsApiClient {
  ReportsApiClient(this._dio);

  final Dio _dio;

  Future<ReportDetail> createReport(CreateReportRequest request) async {
    final response = await _dio.post(
      '/api/reports',
      data: request.toJson(),
    );
    return ReportDetail.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ReportDetail> getReport(String id) async {
    final response = await _dio.get('/api/reports/$id');
    return ReportDetail.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ReportDetail> updateLocation({
    required String id,
    required double latitude,
    required double longitude,
    double? accuracyMeters,
  }) async {
    final response = await _dio.post(
      '/api/reports/$id/location',
      data: {
        'latitude': latitude,
        'longitude': longitude,
        // ignore: use_null_aware_elements
        if (accuracyMeters != null) 'accuracyMeters': accuracyMeters,
      },
    );
    return ReportDetail.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ReportAttachment> uploadAttachment({
    required String reportId,
    required List<int> bytes,
    required String fileName,
    String? contentType,
  }) async {
    final formData = FormData.fromMap({
      'file': MultipartFile.fromBytes(
        bytes,
        filename: fileName,
        contentType: contentType != null ? DioMediaType.parse(contentType) : null,
      ),
    });

    final response = await _dio.post(
      '/api/reports/$reportId/attachments',
      data: formData,
      options: Options(
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      ),
    );
    return ReportAttachment.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ReportDetail> sendSupplementalMessage({
    required String reportId,
    required String message,
  }) async {
    final response = await _dio.post(
      '/api/reports/$reportId/messages',
      data: {'message': message},
    );
    return ReportDetail.fromJson(response.data as Map<String, dynamic>);
  }

  Future<StreamSession> startLiveStream({
    required String reportId,
    required String mode,
  }) async {
    final response = await _dio.post(
      '/api/reports/$reportId/stream',
      data: {'mode': mode},
    );
    return StreamSession.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ReportDetail> endLiveStream({
    required String reportId,
  }) async {
    final response = await _dio.post(
      '/api/reports/$reportId/stream/end',
    );
    return ReportDetail.fromJson(response.data as Map<String, dynamic>);
  }
}
