export type MapStyleType = 'light' | 'streets';

export const MAP_STYLES: Record<MapStyleType, string> = {
  light: 'https://tiles.openfreemap.org/styles/positron',
  streets: 'https://tiles.openfreemap.org/styles/liberty',
};

export const FALLBACK_MAP_STYLE = MAP_STYLES.streets;

/**
 * Custom hook to resolve MapLibre style JSON URL for OpenFreeMap (no API key required).
 */
export const useMapTileStyle = (styleName: MapStyleType = 'light'): string => {
  return MAP_STYLES[styleName] ?? MAP_STYLES.light;
};
