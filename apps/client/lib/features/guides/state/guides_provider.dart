import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:krizovka_client/features/guides/data/guides_repository.dart';
import 'package:krizovka_client/features/guides/models/guide_models.dart';
import 'package:krizovka_client/features/profile/profile_provider.dart';

class GuidesState {
  const GuidesState({
    this.isLoading = false,
    this.guides = const [],
    this.isOffline = false,
    this.searchQuery = '',
    this.selectedCategory,
    this.errorMessage,
  });

  final bool isLoading;
  final List<GuideItem> guides;
  final bool isOffline;
  final String searchQuery;
  final String? selectedCategory;
  final String? errorMessage;

  List<GuideItem> get filteredGuides {
    return guides.where((g) {
      final matchesSearch = searchQuery.isEmpty ||
          g.title.toLowerCase().contains(searchQuery.toLowerCase()) ||
          g.summary.toLowerCase().contains(searchQuery.toLowerCase()) ||
          g.category.toLowerCase().contains(searchQuery.toLowerCase());

      final matchesCategory =
          selectedCategory == null || selectedCategory == 'Vše' || g.category == selectedCategory;

      return matchesSearch && matchesCategory;
    }).toList();
  }

  GuidesState copyWith({
    bool? isLoading,
    List<GuideItem>? guides,
    bool? isOffline,
    String? searchQuery,
    String? selectedCategory,
    String? errorMessage,
  }) {
    return GuidesState(
      isLoading: isLoading ?? this.isLoading,
      guides: guides ?? this.guides,
      isOffline: isOffline ?? this.isOffline,
      searchQuery: searchQuery ?? this.searchQuery,
      selectedCategory: selectedCategory ?? this.selectedCategory,
      errorMessage: errorMessage ?? this.errorMessage,
    );
  }
}

class GuidesNotifier extends Notifier<GuidesState> {
  @override
  GuidesState build() {
    final profile = ref.watch(userProfileProvider);
    final repo = ref.read(guidesRepositoryProvider);
    final initialGuides = repo.getBuiltInFallbackGuides(profile.organizationId);
    return GuidesState(
      isLoading: false,
      guides: initialGuides,
      isOffline: false,
    );
  }

  Future<void> loadGuides(String organizationId) async {
    state = state.copyWith(isLoading: true, errorMessage: null);
    try {
      final repo = ref.read(guidesRepositoryProvider);
      final result = await repo.getGuides(organizationId: organizationId);
      state = state.copyWith(
        isLoading: false,
        guides: result.guides,
        isOffline: result.isFromCache,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: e.toString(),
      );
    }
  }

  void setSearchQuery(String query) {
    state = state.copyWith(searchQuery: query);
  }

  void setCategory(String? category) {
    state = state.copyWith(selectedCategory: category);
  }
}

final guidesNotifierProvider =
    NotifierProvider<GuidesNotifier, GuidesState>(GuidesNotifier.new);
