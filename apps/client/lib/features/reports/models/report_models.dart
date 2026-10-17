class ContactSnapshot {
  const ContactSnapshot({
    this.firstName,
    this.lastName,
    this.personnelNumber,
    this.phone,
    this.email,
  });

  final String? firstName;
  final String? lastName;
  final String? personnelNumber;
  final String? phone;
  final String? email;

  Map<String, dynamic> toJson() => {
    if (firstName != null) 'firstName': firstName,
    if (lastName != null) 'lastName': lastName,
    if (personnelNumber != null) 'personnelNumber': personnelNumber,
    if (phone != null) 'phone': phone,
    if (email != null) 'email': email,
  };

  factory ContactSnapshot.fromJson(Map<String, dynamic> json) => ContactSnapshot(
    firstName: json['firstName'] as String?,
    lastName: json['lastName'] as String?,
    personnelNumber: json['personnelNumber'] as String?,
    phone: json['phone'] as String?,
    email: json['email'] as String?,
  );
}

class CreateReportRequest {
  const CreateReportRequest({
    required this.clientReportId,
    required this.organizationId,
    this.description,
    this.latitude,
    this.longitude,
    this.accuracyMeters,
    this.locationMeasuredAt,
    this.contact,
  });

  final String clientReportId;
  final String organizationId;
  final String? description;
  final double? latitude;
  final double? longitude;
  final double? accuracyMeters;
  final DateTime? locationMeasuredAt;
  final ContactSnapshot? contact;

  Map<String, dynamic> toJson() => {
    'clientReportId': clientReportId,
    'organizationId': organizationId,
    if (description != null) 'description': description,
    if (latitude != null) 'latitude': latitude,
    if (longitude != null) 'longitude': longitude,
    if (accuracyMeters != null) 'accuracyMeters': accuracyMeters,
    if (locationMeasuredAt != null)
      'locationMeasuredAt': locationMeasuredAt!.toUtc().toIso8601String(),
    if (contact != null) 'contact': contact!.toJson(),
  };
}

class ReportDetail {
  const ReportDetail({
    required this.id,
    required this.organizationId,
    required this.clientReportId,
    this.description,
    required this.status,
    required this.createdAt,
    this.updatedAt,
    this.latitude,
    this.longitude,
    this.accuracyMeters,
    this.locationMeasuredAt,
    this.contact,
    this.attachments = const [],
    this.history = const [],
  });

  final String id;
  final String organizationId;
  final String clientReportId;
  final String? description;
  final String status;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final double? latitude;
  final double? longitude;
  final double? accuracyMeters;
  final DateTime? locationMeasuredAt;
  final ContactSnapshot? contact;
  final List<ReportAttachment> attachments;
  final List<ReportHistoryItem> history;

  bool get isNew => status.toLowerCase() == 'new';
  bool get isAccepted => status.toLowerCase() == 'accepted';
  bool get isClosed => status.toLowerCase() == 'closed';

  factory ReportDetail.fromJson(Map<String, dynamic> json) => ReportDetail(
    id: json['id'] as String,
    organizationId: json['organizationId'] as String,
    clientReportId: json['clientReportId'] as String,
    description: json['description'] as String?,
    status: json['status'] as String? ?? 'New',
    createdAt: DateTime.parse(json['createdAt'] as String),
    updatedAt: json['updatedAt'] != null
        ? DateTime.parse(json['updatedAt'] as String)
        : null,
    latitude: (json['latitude'] as num?)?.toDouble(),
    longitude: (json['longitude'] as num?)?.toDouble(),
    accuracyMeters: (json['accuracyMeters'] as num?)?.toDouble(),
    locationMeasuredAt: json['locationMeasuredAt'] != null
        ? DateTime.parse(json['locationMeasuredAt'] as String)
        : null,
    contact: json['contact'] != null
        ? ContactSnapshot.fromJson(json['contact'] as Map<String, dynamic>)
        : null,
    attachments: (json['attachments'] as List?)
            ?.map((a) => ReportAttachment.fromJson(a as Map<String, dynamic>))
            .toList() ??
        const [],
    history: (json['history'] as List?)
            ?.map((h) => ReportHistoryItem.fromJson(h as Map<String, dynamic>))
            .toList() ??
        const [],
  );
}

class ReportHistoryItem {
  const ReportHistoryItem({
    required this.id,
    required this.actorId,
    required this.action,
    this.previousValue,
    this.newValue,
    this.metadata,
    required this.createdAt,
  });

  final String id;
  final String actorId;
  final String action;
  final String? previousValue;
  final String? newValue;
  final String? metadata;
  final DateTime createdAt;

  factory ReportHistoryItem.fromJson(Map<String, dynamic> json) => ReportHistoryItem(
    id: json['id'] as String,
    actorId: json['actorId'] as String? ?? '',
    action: json['action'] as String? ?? '',
    previousValue: json['previousValue'] as String?,
    newValue: json['newValue'] as String?,
    metadata: json['metadata'] as String?,
    createdAt: DateTime.parse(json['createdAt'] as String),
  );
}

class StreamSession {
  const StreamSession({
    required this.reportId,
    required this.roomName,
    required this.token,
    required this.serverUrl,
    required this.mode,
    required this.uplinkOnly,
    required this.expiresAt,
  });

  final String reportId;
  final String roomName;
  final String token;
  final String serverUrl;
  final String mode;
  final bool uplinkOnly;
  final DateTime expiresAt;

  factory StreamSession.fromJson(Map<String, dynamic> json) => StreamSession(
    reportId: json['reportId'] as String,
    roomName: json['roomName'] as String,
    token: json['token'] as String,
    serverUrl: json['serverUrl'] as String,
    mode: json['mode'] as String,
    uplinkOnly: json['uplinkOnly'] as bool? ?? true,
    expiresAt: DateTime.parse(json['expiresAt'] as String),
  );
}

class ReportAttachment {
  const ReportAttachment({
    required this.id,
    required this.fileName,
    required this.contentType,
    required this.sizeBytes,
    required this.storageKey,
    required this.createdAt,
  });

  final String id;
  final String fileName;
  final String contentType;
  final int sizeBytes;
  final String storageKey;
  final DateTime createdAt;

  factory ReportAttachment.fromJson(Map<String, dynamic> json) => ReportAttachment(
    id: json['id'] as String,
    fileName: json['fileName'] as String,
    contentType: json['contentType'] as String,
    sizeBytes: (json['sizeBytes'] as num).toInt(),
    storageKey: json['storageKey'] as String,
    createdAt: DateTime.parse(json['createdAt'] as String),
  );

  Map<String, dynamic> toJson() => {
    'id': id,
    'fileName': fileName,
    'contentType': contentType,
    'sizeBytes': sizeBytes,
    'storageKey': storageKey,
    'createdAt': createdAt.toIso8601String(),
  };
}
