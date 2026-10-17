import 'dart:convert';
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:krizovka_client/features/guides/models/guide_models.dart';
import 'package:krizovka_client/features/reports/data/reports_api_client.dart';

final guidesRepositoryProvider = Provider<GuidesRepository>((ref) {
  final dio = ref.watch(dioProvider);
  return GuidesRepository(dio);
});

class GuidesFetchResult {
  const GuidesFetchResult({
    required this.guides,
    required this.isFromCache,
  });

  final List<GuideItem> guides;
  final bool isFromCache;
}

class GuidesRepository {
  GuidesRepository(this._dio);

  final Dio _dio;

  Future<GuidesFetchResult> getGuides({required String organizationId}) async {
    final cacheKey = 'offline_guides_$organizationId';

    try {
      final response = await _dio.get(
        '/api/guides',
        queryParameters: {
          'organizationId': organizationId,
          'status': 'Published',
        },
      );

      final list = (response.data as List<dynamic>)
          .map((item) => GuideItem.fromJson(item as Map<String, dynamic>))
          .toList();

      // Persist to offline cache
      try {
        final prefs = await SharedPreferences.getInstance();
        final jsonStr = jsonEncode(list.map((g) => g.toJson()).toList());
        await prefs.setString(cacheKey, jsonStr);
      } catch (_) {
        // Cache write non-fatal
      }

      return GuidesFetchResult(guides: list, isFromCache: false);
    } catch (e) {
      // Offline fallback: try reading from SharedPreferences cache
      try {
        final prefs = await SharedPreferences.getInstance();
        final cachedStr = prefs.getString(cacheKey);
        if (cachedStr != null && cachedStr.isNotEmpty) {
          final decoded = jsonDecode(cachedStr) as List<dynamic>;
          final cachedGuides = decoded
              .map((item) => GuideItem.fromJson(item as Map<String, dynamic>))
              .toList();
          return GuidesFetchResult(guides: cachedGuides, isFromCache: true);
        }
      } catch (_) {}

      // Hard fallback if completely empty and offline
      return GuidesFetchResult(
        guides: getBuiltInFallbackGuides(organizationId),
        isFromCache: true,
      );
    }
  }

  List<GuideItem> getBuiltInFallbackGuides(String organizationId) {
    return [
      GuideItem(
        id: 'fallback-shooter',
        organizationId: organizationId,
        title: 'Aktivní střelec / Ozbrojený útok (Offline manuál)',
        category: 'Bezpečnostní hrozby',
        summary: 'Pravidlo UTEČ - SCHOVEJ SE - BOJUJ pro případ útoku v budově.',
        status: 'Published',
        steps: const [
          GuideStep(
            stepNumber: 1,
            title: 'UTEČ',
            description: 'Pokud je volná bezpečná cesta, opusť budovu. Neber věci. Varuj ostatní.',
          ),
          GuideStep(
            stepNumber: 2,
            title: 'SCHOVEJ SE',
            description: 'Zamkni a zataras dveře těžkým nábytkem. Zhasni a ztiš telefon.',
          ),
          GuideStep(
            stepNumber: 3,
            title: 'BOJUJ',
            description: 'Až jako poslední možnost v přímém ohrožení života. Využij židli či hasicí přístroj.',
          ),
          GuideStep(
            stepNumber: 4,
            title: 'ZÁKAZ: Neshlukovat se',
            description: 'Neshlukujte se u oken a hlavních vstupů.',
            isDoNot: true,
          ),
        ],
        emergencyContacts: const [
          EmergencyContact(name: 'Policie ČR', phone: '158', role: 'Policie'),
          EmergencyContact(name: 'Tísňová linka', phone: '112', role: 'IZS'),
        ],
      ),
      GuideItem(
        id: 'fallback-fire',
        organizationId: organizationId,
        title: 'Požár a evakuace (Offline manuál)',
        category: 'Živelní události',
        summary: 'Zásady bezpečného opuštění budovy při požárním poplachu.',
        status: 'Published',
        steps: const [
          GuideStep(
            stepNumber: 1,
            title: 'VYHLAŠ POPLACH',
            description: 'Stiskni požární hlásič a volej 150/112.',
          ),
          GuideStep(
            stepNumber: 2,
            title: 'EVAKUACE',
            description: 'Postupuj po únikových cestách na určené shromaždiště.',
          ),
          GuideStep(
            stepNumber: 3,
            title: 'ZÁKAZ: Výtahy',
            description: 'Za žádných okolností nepoužívejte výtahy.',
            isDoNot: true,
          ),
        ],
        emergencyContacts: const [
          EmergencyContact(name: 'Hasiči', phone: '150', role: 'Hasiči'),
          EmergencyContact(name: 'Tísňová linka', phone: '112', role: 'IZS'),
        ],
      ),
    ];
  }
}
