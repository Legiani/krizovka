'use client';

import React, { useMemo } from 'react';
import { Source, Layer, type LayerProps } from 'react-map-gl/maplibre';
import type { FeatureCollection, Point } from 'geojson';
import type { DisplayReport } from '../types';
import type { ReportStatus } from '@/demo/types';

interface ReportMapLayerProps {
  reports: DisplayReport[];
  selectedStatuses: ReportStatus[];
}

const clusterLayer: LayerProps = {
  id: 'report-clusters',
  type: 'circle',
  source: 'reports-source',
  filter: ['has', 'point_count'],
  paint: {
    'circle-color': [
      'step',
      ['get', 'point_count'],
      '#51bbd6',
      10,
      '#f1f075',
      30,
      '#f28cb1',
    ],
    'circle-radius': [
      'step',
      ['get', 'point_count'],
      18,
      10,
      25,
      30,
      35,
    ],
    'circle-stroke-width': 2,
    'circle-stroke-color': '#ffffff',
  },
};

const clusterCountLayer: LayerProps = {
  id: 'report-cluster-count',
  type: 'symbol',
  source: 'reports-source',
  filter: ['has', 'point_count'],
  layout: {
    'text-field': '{point_count}',
    'text-size': 12,
  },
  paint: {
    'text-color': '#1f2937',
  },
};

const unclusteredPointLayer: LayerProps = {
  id: 'report-unclustered-point',
  type: 'circle',
  source: 'reports-source',
  filter: ['!', ['has', 'point_count']],
  paint: {
    'circle-color': [
      'match',
      ['get', 'status'],
      'New',
      '#dc3545',
      'Accepted',
      '#ffc107',
      'Closed',
      '#198754',
      '#6c757d',
    ],
    'circle-radius': 9,
    'circle-stroke-width': 2.5,
    'circle-stroke-color': '#ffffff',
  },
};

export const ReportMapLayer: React.FC<ReportMapLayerProps> = ({
  reports,
  selectedStatuses,
}) => {
  const geojsonData: FeatureCollection<Point> = useMemo(() => {
    const statusFiltered =
      selectedStatuses.length > 0
        ? reports.filter(r => selectedStatuses.includes(r.status))
        : reports;

    const validCoordReports = statusFiltered.filter(
      r =>
        r.latitude !== undefined &&
        r.latitude !== null &&
        !isNaN(r.latitude) &&
        r.longitude !== undefined &&
        r.longitude !== null &&
        !isNaN(r.longitude)
    );

    return {
      type: 'FeatureCollection',
      features: validCoordReports.map(report => ({
        type: 'Feature',
        geometry: {
          type: 'Point',
          coordinates: [report.longitude!, report.latitude!],
        },
        properties: {
          id: report.id,
          text: report.text,
          place: report.place,
          status: report.status,
          channel: report.channel,
          createdAt: report.createdAt,
          isLive: report.isLive ? 1 : 0,
          reporterName: report.reporterName || '',
          reporterPhone: report.reporterPhone || '',
          reporterEmail: report.reporterEmail || '',
          reporterPersonnelNumber: report.reporterPersonnelNumber || '',
          latitude: report.latitude,
          longitude: report.longitude,
        },
      })),
    };
  }, [reports, selectedStatuses]);

  return (
    <Source
      id="reports-source"
      type="geojson"
      data={geojsonData}
      cluster={true}
      clusterMaxZoom={14}
      clusterRadius={50}
    >
      <Layer {...clusterLayer} />
      <Layer {...clusterCountLayer} />
      <Layer {...unclusteredPointLayer} />
    </Source>
  );
};
