import 'package:flutter/foundation.dart';

class ImageSourceData {
  final String? path;
  final Uint8List? bytes;

  const ImageSourceData({this.path, this.bytes});

  bool get isWeb => kIsWeb;
  bool get hasBytes => bytes != null;
  bool get hasPath => path != null && path!.isNotEmpty;
}
