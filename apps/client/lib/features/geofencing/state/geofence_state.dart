import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:krizovka_client/core/services/location_service.dart';
import 'package:krizovka_client/features/geofencing/data/geofence_api_client.dart';
import 'package:krizovka_client/features/geofencing/models/geofence_models.dart';
import 'package:krizovka_client/features/profile/profile_provider.dart';

class GeofenceState {
  const GeofenceState({
    this.isLoading = false,
    this.result,
    this.errorMessage,
  });

  final bool isLoading;
  final ResolveLocationResult? result;
  final String? errorMessage;

  String get displayLabel {
    if (isLoading) return 'Aktivní krizový řetězec: Zjišťuji perimetr a alokovaný velín...';
    if (result == null) {
      return 'Aktivní krizový řetězec: hlášení se odesílají do centrálního velínu (.NET 10 API).';
    }

    if (result!.isFallback) {
      return 'Aktivní krizový řetězec: 📍 Výchozí dispečink (${result!.organizationName})';
    }

    final area = result!.areaName ?? 'Areál';
    final offsetNote = result!.isWithinOffset ? ' [Perimetr/parkoviště]' : '';
    return 'Aktivní krizový řetězec: 📍 $area$offsetNote · Velín: ${result!.allocatedVelinId}';
  }

  GeofenceState copyWith({
    bool? isLoading,
    ResolveLocationResult? result,
    String? errorMessage,
  }) {
    return GeofenceState(
      isLoading: isLoading ?? this.isLoading,
      result: result ?? this.result,
      errorMessage: errorMessage ?? this.errorMessage,
    );
  }
}

class GeofenceNotifier extends Notifier<GeofenceState> {
  @override
  GeofenceState build() {
    return const GeofenceState();
  }

  Future<void> bootstrap({double? lat, double? lon}) async {
    state = state.copyWith(isLoading: true, errorMessage: null);
    try {
      double resolvedLat;
      double resolvedLon;

      if (lat != null && lon != null) {
        resolvedLat = lat;
        resolvedLon = lon;
      } else {
        final locationService = ref.read(locationServiceProvider);
        final loc = await locationService.getCurrentPositionSafe();
        if (loc != null) {
          resolvedLat = loc.latitude;
          resolvedLon = loc.longitude;
        } else {
          resolvedLat = 50.087811;
          resolvedLon = 14.420460;
        }
      }

      final client = ref.read(geofenceApiClientProvider);
      final res = await client.resolveLocation(latitude: resolvedLat, longitude: resolvedLon);

      // Silently adapt active organization context without interrupting user
      ref.read(userProfileProvider.notifier).updateProfile(
            organizationId: res.organizationId,
            organizationName: res.organizationName,
          );

      state = state.copyWith(isLoading: false, result: res);
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: e.toString(),
      );
    }
  }
}

final geofenceNotifierProvider =
    NotifierProvider<GeofenceNotifier, GeofenceState>(GeofenceNotifier.new);
