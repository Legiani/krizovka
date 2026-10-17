'use client';

import React, { useState, useRef, useMemo, useEffect, useCallback } from 'react';
import Map, {
  NavigationControl,
  GeolocateControl,
  type MapRef,
  type MapLayerMouseEvent,
} from 'react-map-gl/maplibre';
import * as maplibregl from 'maplibre-gl';
import 'maplibre-gl/dist/maplibre-gl.css';
import { Dropdown, ButtonGroup, Button } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faMap, faCrosshairs } from '@fortawesome/free-solid-svg-icons';
import { useMapTileStyle, type MapStyleType } from './hooks/useMapTileStyle';
import { ReportMapLayer } from './components/ReportMapLayer';
import { MapSidebar } from './components/MapSidebar';
import { ReportDetailMapPanel } from './components/ReportDetailMapPanel';
import type { DisplayReport, SelectedMapReport } from './types';
import type { ReportStatus } from '@/demo/types';

interface IncidentMapModuleProps {
  reports: DisplayReport[];
  onSelectReport: (reportId: string) => void;
  selectedReportId?: string | null;
  height?: string | number;
}

const INTERACTIVE_LAYER_IDS = ['report-clusters', 'report-unclustered-point'];

export const IncidentMapModule: React.FC<IncidentMapModuleProps> = ({
  reports,
  onSelectReport,
  selectedReportId,
  height = 'calc(100vh - 240px)',
}) => {
  const mapRef = useRef<MapRef>(null);

  const [showReports, setShowReports] = useState(true);
  const [selectedStatuses, setSelectedStatuses] = useState<ReportStatus[]>([]);
  const [selectedPlace, setSelectedPlace] = useState<SelectedMapReport | null>(null);
  const [baseMapStyle, setBaseMapStyle] = useState<MapStyleType>('light');
  const [cursor, setCursor] = useState<string>('auto');

  const tileStyle = useMapTileStyle(baseMapStyle);

  // Initial center based on available reports or fallback to Prague
  const initialCenter = useMemo(() => {
    const validReports = reports.filter(
      r =>
        r.latitude !== undefined &&
        r.latitude !== null &&
        r.longitude !== undefined &&
        r.longitude !== null
    );

    if (validReports.length > 0) {
      const avgLat =
        validReports.reduce((acc, r) => acc + (r.latitude || 0), 0) / validReports.length;
      const avgLng =
        validReports.reduce((acc, r) => acc + (r.longitude || 0), 0) / validReports.length;
      return { longitude: avgLng, latitude: avgLat, zoom: 12.5 };
    }

    return { longitude: 14.4378, latitude: 50.0755, zoom: 12 };
  }, [reports]);

  // Derive active place from user map selection or external selectedReportId
  const activePlace = useMemo<SelectedMapReport | null>(() => {
    if (selectedPlace) return selectedPlace;
    if (!selectedReportId) return null;
    const found = reports.find(r => r.id === selectedReportId);
    if (!found || found.latitude == null || found.longitude == null) return null;
    return {
      id: found.id,
      title: found.text || 'Nouzové hlášení',
      description: found.text || '',
      place: found.place || '',
      status: found.status,
      channel: found.channel || 'Klient (SOS)',
      createdAt: found.createdAt,
      isLive: found.isLive,
      latitude: found.latitude,
      longitude: found.longitude,
      reporterName: found.reporterName,
      reporterPhone: found.reporterPhone,
    };
  }, [selectedPlace, selectedReportId, reports]);

  // Fly to selected report coordinates when external selection changes
  useEffect(() => {
    if (!selectedReportId) return;
    const found = reports.find(r => r.id === selectedReportId);
    if (found?.latitude && found?.longitude) {
      mapRef.current?.flyTo({
        center: [found.longitude, found.latitude],
        zoom: Math.max(14, mapRef.current.getMap().getZoom() || 12),
        essential: true,
      });
    }
  }, [selectedReportId, reports]);

  const onMapClick = useCallback((event: MapLayerMouseEvent) => {
    const feature = event.features?.[0];
    if (!feature) {
      setSelectedPlace(null);
      return;
    }

    // Cluster click → zoom in
    if (feature.layer?.id === 'report-clusters') {
      const coords = event.lngLat;
      const currentZoom = mapRef.current?.getMap().getZoom() ?? 12;
      mapRef.current?.flyTo({
        center: [coords.lng, coords.lat],
        zoom: currentZoom + 2,
        duration: 500,
      });
      return;
    }

    // Unclustered point click → show side detail
    if (feature.layer?.id === 'report-unclustered-point') {
      const props = (feature.properties || {}) as Record<string, unknown>;
      const geom = feature.geometry as { type: 'Point'; coordinates: [number, number] };
      const lng = geom?.coordinates?.[0] ?? event.lngLat.lng;
      const lat = geom?.coordinates?.[1] ?? event.lngLat.lat;

      setSelectedPlace({
        id: String(props.id || ''),
        title: String(props.text || 'Nouzové hlášení'),
        description: String(props.text || ''),
        place: String(props.place || ''),
        status: props.status as ReportStatus,
        channel: String(props.channel || 'Klient (SOS)'),
        createdAt: String(props.createdAt || new Date().toISOString()),
        isLive: Boolean(props.isLive),
        latitude: lat,
        longitude: lng,
        reporterName: props.reporterName ? String(props.reporterName) : undefined,
        reporterPhone: props.reporterPhone ? String(props.reporterPhone) : undefined,
      });

      mapRef.current?.easeTo({
        center: [lng, lat],
        duration: 400,
      });
    }
  }, []);

  const onMouseEnter = useCallback(() => setCursor('pointer'), []);
  const onMouseLeave = useCallback(() => setCursor('auto'), []);

  const handleResetView = () => {
    mapRef.current?.flyTo({
      center: [initialCenter.longitude, initialCenter.latitude],
      zoom: initialCenter.zoom,
      duration: 600,
    });
  };

  return (
    <div
      className="position-relative w-100 rounded-3 overflow-hidden border shadow-sm bg-light"
      style={{ height, minHeight: 480 }}
    >
      {/* Floating Left Sidebar */}
      <MapSidebar
        showReports={showReports}
        onToggleReports={setShowReports}
        selectedStatuses={selectedStatuses}
        onSelectedStatusesChange={setSelectedStatuses}
        reports={reports}
      />

      {/* Quick Center Reset Button */}
      <div
        className="position-absolute z-2"
        style={{ top: 15, right: 55 }}
      >
        <Button
          variant="light"
          size="sm"
          className="shadow-sm border rounded bg-white text-secondary py-1 px-2"
          onClick={handleResetView}
          title="Vystředit mapu na incidenty"
        >
          <FontAwesomeIcon icon={faCrosshairs} className="me-1" />
          Vystředit
        </Button>
      </div>

      {/* Floating Basemap Selector bottom-right */}
      <div
        className="position-absolute z-2"
        style={{
          bottom: 20,
          right: activePlace ? 395 : 15,
          transition: 'right 0.25s ease-out',
        }}
      >
        <Dropdown as={ButtonGroup} size="sm">
          <Dropdown.Toggle
            variant="light"
            className="shadow-sm border bg-white d-flex align-items-center gap-1"
          >
            <FontAwesomeIcon icon={faMap} className="text-secondary" />
            <span>Podklad: {baseMapStyle === 'light' ? 'Světlý' : 'Ulice'}</span>
          </Dropdown.Toggle>
          <Dropdown.Menu className="shadow-sm" align="end">
            <Dropdown.Item
              active={baseMapStyle === 'light'}
              onClick={() => setBaseMapStyle('light')}
            >
              Světlý (Positron)
            </Dropdown.Item>
            <Dropdown.Item
              active={baseMapStyle === 'streets'}
              onClick={() => setBaseMapStyle('streets')}
            >
              Uliční (Liberty)
            </Dropdown.Item>
          </Dropdown.Menu>
        </Dropdown>
      </div>

      {/* MapLibre React Map */}
      <Map
        ref={mapRef}
        mapLib={maplibregl}
        initialViewState={{
          longitude: initialCenter.longitude,
          latitude: initialCenter.latitude,
          zoom: initialCenter.zoom,
        }}
        style={{ width: '100%', height: '100%' }}
        mapStyle={tileStyle}
        attributionControl={false}
        interactiveLayerIds={INTERACTIVE_LAYER_IDS}
        onClick={onMapClick}
        onMouseEnter={onMouseEnter}
        onMouseLeave={onMouseLeave}
        cursor={cursor}
      >
        <NavigationControl position="top-right" />
        <GeolocateControl position="top-right" />

        {showReports && (
          <ReportMapLayer
            reports={reports}
            selectedStatuses={selectedStatuses}
          />
        )}
      </Map>

      {/* Right Sliding Detail Panel */}
      {activePlace && (
        <ReportDetailMapPanel
          report={activePlace}
          onClose={() => {
            setSelectedPlace(null);
          }}
          onOpenDetail={reportId => {
            onSelectReport(reportId);
          }}
        />
      )}
    </div>
  );
};

export default IncidentMapModule;
