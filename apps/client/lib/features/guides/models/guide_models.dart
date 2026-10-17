import 'dart:convert';

class GuideStep {
  const GuideStep({
    required this.stepNumber,
    required this.title,
    required this.description,
    this.isDoNot = false,
  });

  final int stepNumber;
  final String title;
  final String description;
  final bool isDoNot;

  factory GuideStep.fromJson(Map<String, dynamic> json) {
    return GuideStep(
      stepNumber: json['stepNumber'] as int? ?? 1,
      title: json['title'] as String? ?? '',
      description: json['description'] as String? ?? '',
      isDoNot: json['isDoNot'] as bool? ?? false,
    );
  }

  Map<String, dynamic> toJson() => {
        'stepNumber': stepNumber,
        'title': title,
        'description': description,
        'isDoNot': isDoNot,
      };
}

class EmergencyContact {
  const EmergencyContact({
    required this.name,
    required this.phone,
    required this.role,
  });

  final String name;
  final String phone;
  final String role;

  factory EmergencyContact.fromJson(Map<String, dynamic> json) {
    return EmergencyContact(
      name: json['name'] as String? ?? '',
      phone: json['phone'] as String? ?? '',
      role: json['role'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
        'name': name,
        'phone': phone,
        'role': role,
      };
}

class LegalBasisRef {
  const LegalBasisRef({
    required this.title,
    this.lawNumber,
    this.section,
    this.issuer,
    this.url,
  });

  final String title;
  final String? lawNumber;
  final String? section;
  final String? issuer;
  final String? url;

  factory LegalBasisRef.fromJson(Map<String, dynamic> json) {
    return LegalBasisRef(
      title: json['title'] as String? ?? '',
      lawNumber: json['lawNumber'] as String?,
      section: json['section'] as String?,
      issuer: json['issuer'] as String?,
      url: json['url'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
        'title': title,
        if (lawNumber != null) 'lawNumber': lawNumber,
        if (section != null) 'section': section,
        if (issuer != null) 'issuer': issuer,
        if (url != null) 'url': url,
      };
}

class GuideItem {
  const GuideItem({
    required this.id,
    required this.organizationId,
    required this.title,
    required this.category,
    required this.summary,
    this.quickSummary = '',
    this.diagramMermaid = '',
    required this.status,
    required this.steps,
    required this.emergencyContacts,
    this.legalBasis = const [],
  });

  final String id;
  final String organizationId;
  final String title;
  final String category;
  final String summary;
  final String quickSummary;
  final String diagramMermaid;
  final String status;
  final List<GuideStep> steps;
  final List<EmergencyContact> emergencyContacts;
  final List<LegalBasisRef> legalBasis;

  factory GuideItem.fromJson(Map<String, dynamic> json) {
    List<GuideStep> steps = [];
    if (json['stepsJson'] != null && json['stepsJson'] is String) {
      try {
        final decoded = jsonDecode(json['stepsJson'] as String) as List<dynamic>;
        steps = decoded.map((s) => GuideStep.fromJson(s as Map<String, dynamic>)).toList();
      } catch (_) {}
    } else if (json['steps'] != null && json['steps'] is List) {
      steps = (json['steps'] as List<dynamic>)
          .map((s) => GuideStep.fromJson(s as Map<String, dynamic>))
          .toList();
    }

    List<EmergencyContact> contacts = [];
    if (json['emergencyContactsJson'] != null && json['emergencyContactsJson'] is String) {
      try {
        final decoded = jsonDecode(json['emergencyContactsJson'] as String) as List<dynamic>;
        contacts = decoded.map((c) => EmergencyContact.fromJson(c as Map<String, dynamic>)).toList();
      } catch (_) {}
    } else if (json['emergencyContacts'] != null && json['emergencyContacts'] is List) {
      contacts = (json['emergencyContacts'] as List<dynamic>)
          .map((c) => EmergencyContact.fromJson(c as Map<String, dynamic>))
          .toList();
    }

    List<LegalBasisRef> legals = [];
    if (json['legalBasisJson'] != null && json['legalBasisJson'] is String) {
      try {
        final decoded = jsonDecode(json['legalBasisJson'] as String) as List<dynamic>;
        legals = decoded.map((l) => LegalBasisRef.fromJson(l as Map<String, dynamic>)).toList();
      } catch (_) {}
    } else if (json['legalBasis'] != null && json['legalBasis'] is List) {
      legals = (json['legalBasis'] as List<dynamic>)
          .map((l) => LegalBasisRef.fromJson(l as Map<String, dynamic>))
          .toList();
    }

    return GuideItem(
      id: json['id'] as String,
      organizationId: json['organizationId'] as String,
      title: json['title'] as String? ?? '',
      category: json['category'] as String? ?? '',
      summary: json['summary'] as String? ?? '',
      quickSummary: json['quickSummary'] as String? ?? '',
      diagramMermaid: json['diagramMermaid'] as String? ?? '',
      status: json['status'] as String? ?? 'Published',
      steps: steps,
      emergencyContacts: contacts,
      legalBasis: legals,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'organizationId': organizationId,
        'title': title,
        'category': category,
        'summary': summary,
        'quickSummary': quickSummary,
        'diagramMermaid': diagramMermaid,
        'status': status,
        'steps': steps.map((s) => s.toJson()).toList(),
        'emergencyContacts': emergencyContacts.map((c) => c.toJson()).toList(),
        'legalBasis': legalBasis.map((l) => l.toJson()).toList(),
      };
}
