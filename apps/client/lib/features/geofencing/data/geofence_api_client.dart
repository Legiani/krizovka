import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:krizovka_client/features/geofencing/models/geofence_models.dart';
import 'package:krizovka_client/features/reports/data/reports_api_client.dart';

final geofenceApiClientProvider = Provider<GeofenceApiClient>((ref) {
  final dio = ref.watch(dioProvider);
  return GeofenceApiClient(dio);
});

class GeofenceApiClient {
  GeofenceApiClient(this._dio);

  final Dio _dio;

  Future<ResolveLocationResult> resolveLocation({
    required double latitude,
    required double longitude,
  }) async {
    final response = await _dio.get(
      '/api/organizations/resolve-location',
      queryParameters: {
        'lat': latitude,
        'lon': longitude,
      },
    );
    return ResolveLocationResult.fromJson(response.data as Map<String, dynamic>);
  }
}
