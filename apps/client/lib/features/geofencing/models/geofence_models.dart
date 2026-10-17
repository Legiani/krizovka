class ResolveLocationResult {
  const ResolveLocationResult({
    required this.organizationId,
    required this.organizationName,
    this.areaId,
    this.areaName,
    required this.allocatedVelinId,
    required this.isFallback,
    required this.isWithinCore,
    required this.isWithinOffset,
    this.distanceToAreaMeters,
  });

  final String organizationId;
  final String organizationName;
  final String? areaId;
  final String? areaName;
  final String allocatedVelinId;
  final bool isFallback;
  final bool isWithinCore;
  final bool isWithinOffset;
  final double? distanceToAreaMeters;

  factory ResolveLocationResult.fromJson(Map<String, dynamic> json) {
    return ResolveLocationResult(
      organizationId: json['organizationId'] as String,
      organizationName: json['organizationName'] as String,
      areaId: json['areaId'] as String?,
      areaName: json['areaName'] as String?,
      allocatedVelinId: json['allocatedVelinId'] as String,
      isFallback: json['isFallback'] as bool? ?? false,
      isWithinCore: json['isWithinCore'] as bool? ?? false,
      isWithinOffset: json['isWithinOffset'] as bool? ?? false,
      distanceToAreaMeters: (json['distanceToAreaMeters'] as num?)?.toDouble(),
    );
  }
}
