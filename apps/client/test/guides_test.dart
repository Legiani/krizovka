import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:krizovka_client/features/guides/models/guide_models.dart';
import 'package:krizovka_client/features/guides/state/guides_provider.dart';

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  test('GuidesState filter matching query and category', () {
    const list = [
      GuideItem(
        id: '1',
        organizationId: 'org1',
        title: 'Aktivní střelec v budově',
        category: 'Bezpečnostní hrozby',
        summary: 'Uteč, schovej se, bojuj.',
        status: 'Published',
        steps: [
          GuideStep(stepNumber: 1, title: 'UTEČ', description: 'Rychle opusť budovu.'),
          GuideStep(stepNumber: 2, title: 'ZÁKAZ: Okna', description: 'Nestůj u oken.', isDoNot: true),
        ],
        emergencyContacts: [
          EmergencyContact(name: 'Policie ČR', phone: '158', role: 'Policie'),
        ],
      ),
      GuideItem(
        id: '2',
        organizationId: 'org1',
        title: 'Evakuace při požáru',
        category: 'Živelní události',
        summary: 'Použijte schodiště a opusťte objekt.',
        status: 'Published',
        steps: [
          GuideStep(stepNumber: 1, title: 'HLÁSIČ', description: 'Stiskni hlásič.'),
        ],
        emergencyContacts: [],
      ),
    ];

    const stateAll = GuidesState(guides: list);
    expect(stateAll.filteredGuides.length, 2);

    // Search query
    final stateSearch = stateAll.copyWith(searchQuery: 'střelec');
    expect(stateSearch.filteredGuides.length, 1);
    expect(stateSearch.filteredGuides.first.id, '1');

    // Category filter
    final stateCat = stateAll.copyWith(selectedCategory: 'Živelní události');
    expect(stateCat.filteredGuides.length, 1);
    expect(stateCat.filteredGuides.first.id, '2');
  });

  test('GuideItem JSON serialization and step parsing', () {
    final original = GuideItem(
      id: 'g-100',
      organizationId: 'org-test',
      title: 'Technická havárie',
      category: 'Technické havárie',
      summary: 'Únik chemikálie v laboratoři.',
      status: 'Published',
      steps: const [
        GuideStep(stepNumber: 1, title: 'MASKA', description: 'Nasaď masku.'),
      ],
      emergencyContacts: const [
        EmergencyContact(name: 'Hasiči', phone: '150', role: 'Hasiči'),
      ],
    );

    final json = original.toJson();
    final restored = GuideItem.fromJson(json);

    expect(restored.id, 'g-100');
    expect(restored.title, 'Technická havárie');
    expect(restored.steps.length, 1);
    expect(restored.steps.first.title, 'MASKA');
    expect(restored.emergencyContacts.length, 1);
    expect(restored.emergencyContacts.first.phone, '150');
  });

  test('GuideItem parses quickSummary, diagramMermaid and legalBasis', () {
    const raw = {
      'id': 'tmpl-met-01-amok',
      'organizationId': 'org-1',
      'title': 'Aktivní střelec (AMOK)',
      'category': 'Bezpečnost a měkké cíle',
      'summary': 'Uteč, schovej se, bojuj.',
      'quickSummary': '1. UTEČ. 2. SCHOVEJ SE. 3. BOJUJ.',
      'diagramMermaid': 'flowchart TD\n  Start --> Run',
      'status': 'Published',
      'stepsJson': '[{"stepNumber":1,"title":"UTEČ","description":"Utíkej"}]',
      'emergencyContactsJson': '[{"name":"Policie","phone":"158","role":"Policie"}]',
      'legalBasisJson': '[{"title":"Metodika OMC","lawNumber":"AMOK 1","url":"https://mvcr.cz"}]',
    };

    final item = GuideItem.fromJson(raw);
    expect(item.quickSummary, '1. UTEČ. 2. SCHOVEJ SE. 3. BOJUJ.');
    expect(item.diagramMermaid, 'flowchart TD\n  Start --> Run');
    expect(item.legalBasis.length, 1);
    expect(item.legalBasis.first.title, 'Metodika OMC');
    expect(item.legalBasis.first.url, 'https://mvcr.cz');
  });

  test('GuidesNotifier provides built-in guides initially for immediate offline access', () {
    final container = ProviderContainer();
    addTearDown(container.dispose);

    final state = container.read(guidesNotifierProvider);
    expect(state.isLoading, false);
    expect(state.guides.isNotEmpty, true);
    expect(state.guides.any((g) => g.title.contains('Aktivní střelec')), true);
  });
}
