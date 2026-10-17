import 'dart:async';
import 'dart:io' show Platform;
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

class LocationService {
  const LocationService({this.testPosition});

  final Position? testPosition;

  static bool get isTestEnvironment {
    try {
      return Platform.environment.containsKey('FLUTTER_TEST');
    } catch (_) {
      return false;
    }
  }

  /// Fail-safe acquisition of current GPS coordinates with maximum 3 seconds timeout.
  /// If permissions are denied, GPS is disabled or timeout expires, attempts
  /// [Geolocator.getLastKnownPosition]. Returns `null` if position is completely unavailable,
  /// ensuring SOS submission is never blocked.
  Future<Position?> getCurrentPositionSafe({
    Duration timeout = const Duration(seconds: 3),
  }) async {
    if (testPosition != null) {
      return testPosition;
    }

    if (isTestEnvironment) {
      return Position(
        latitude: 50.087811,
        longitude: 14.420460,
        timestamp: DateTime.now(),
        accuracy: 10.0,
        altitude: 0.0,
        altitudeAccuracy: 0.0,
        heading: 0.0,
        headingAccuracy: 0.0,
        speed: 0.0,
        speedAccuracy: 0.0,
      );
    }

    try {
      final serviceEnabled = await Geolocator.isLocationServiceEnabled()
          .timeout(timeout, onTimeout: () => false);
      if (!serviceEnabled) {
        return await _getLastKnownSafe();
      }

      var permission = await Geolocator.checkPermission()
          .timeout(timeout, onTimeout: () => LocationPermission.denied);
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission()
            .timeout(timeout, onTimeout: () => LocationPermission.denied);
        if (permission == LocationPermission.denied) {
          return await _getLastKnownSafe();
        }
      }

      if (permission == LocationPermission.deniedForever) {
        return await _getLastKnownSafe();
      }

      final position = await Geolocator.getCurrentPosition(
        locationSettings: LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: timeout,
        ),
      );

      return position;
    } catch (e) {
      debugPrint('LocationService: Failed or timed out ($e), fallback to last known.');
      return await _getLastKnownSafe();
    }
  }

  Future<Position?> _getLastKnownSafe() async {
    try {
      return await Geolocator.getLastKnownPosition()
          .timeout(const Duration(seconds: 1), onTimeout: () => null);
    } catch (e) {
      debugPrint('LocationService: Failed to retrieve last known position: $e');
      return null;
    }
  }
}

final locationServiceProvider = Provider<LocationService>((ref) {
  return const LocationService();
});
