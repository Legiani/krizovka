'use client';

import React from 'react';
import { Badge } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';

export interface StatusEntry {
    label?: string;
    variant: string;
    /** Optional FontAwesome icon rendered before the label */
    icon?: IconDefinition;
}

export type StatusPreset = 'user' | 'provider' | 'issue' | 'article'
    | 'entitlement' | 'journey' | 'chargeProposal' | 'media' | 'deviceKey'
    | 'tariffVersion' | 'eligibility';

const PRESETS: Record<StatusPreset, Record<string, StatusEntry>> = {
    user: {
        ACTIVE: { label: 'Aktivní', variant: 'success' },
        SUSPENDED: { label: 'Zablokován', variant: 'danger' },
        INACTIVE: { label: 'Neaktivní', variant: 'secondary' },
        PENDING: { label: 'Čeká', variant: 'warning' },
    },
    provider: {
        ACTIVE: { label: 'Aktivní', variant: 'success' },
        INACTIVE: { label: 'Neaktivní', variant: 'secondary' },
        PLANNED: { label: 'Připravovaná', variant: 'info' },
        SUNSET: { label: 'Ukončován', variant: 'warning' },
        DECOMMISSIONED: { label: 'Vyřazen', variant: 'danger' },
    },
    issue: {
        New: { label: 'Nový', variant: 'primary' },
        InProgress: { label: 'Zpracovává se', variant: 'warning' },
        Resolved: { label: 'Vyřešen', variant: 'success' },
        Rejected: { label: 'Zamítnut', variant: 'danger' },
        Merged: { label: 'Sloučen', variant: 'secondary' },
        WaitingForInfo: { label: 'Čeká na info', variant: 'info' },
    },
    article: {
        Published: { label: 'Publikováno', variant: 'success' },
        Draft: { label: 'Koncept', variant: 'warning' },
        Pending: { label: 'Čeká na schválení', variant: 'info' },
        Archived: { label: 'Archivováno', variant: 'secondary' },
    },
    // ─── Transport presets ────────────────────────────────────────────────────
    entitlement: {
        INITIATED: { label: 'Iniciováno', variant: 'secondary' },
        ACTIVE: { label: 'Aktivní', variant: 'success' },
        SUSPENDED: { label: 'Pozastaveno', variant: 'warning' },
        REVOKED: { label: 'Revokováno', variant: 'danger' },
        EXPIRED: { label: 'Expirováno', variant: 'dark' },
    },
    journey: {
        Open: { label: 'Otevřená', variant: 'primary' },
        Closed: { label: 'Uzavřená', variant: 'info' },
        Settled: { label: 'Vyúčtována', variant: 'success' },
    },
    chargeProposal: {
        Draft: { label: 'Návrh', variant: 'secondary' },
        Emitted: { label: 'Odesláno', variant: 'info' },
        Settled: { label: 'Vyúčtováno', variant: 'success' },
        Failed: { label: 'Selhalo', variant: 'danger' },
    },
    media: {
        Active: { label: 'Aktivní', variant: 'success' },
        Suspended: { label: 'Pozastaveno', variant: 'warning' },
        Revoked: { label: 'Revokováno', variant: 'danger' },
    },
    deviceKey: {
        Active: { label: 'Aktivní', variant: 'success' },
        Suspended: { label: 'Rotace', variant: 'warning' },
        Revoked: { label: 'Revokován', variant: 'danger' },
    },
    tariffVersion: {
        DRAFT: { label: 'Draft', variant: 'secondary' },
        REVIEWED: { label: 'Ke schválení', variant: 'info' },
        APPROVED: { label: 'Schváleno', variant: 'warning' },
        ACTIVE: { label: 'Aktivní', variant: 'success' },
        DEPRECATED: { label: 'Zastaralé', variant: 'dark' },
    },
    eligibility: {
        Ok: { label: 'V pořádku', variant: 'success' },
        Debt: { label: 'Dluh', variant: 'warning' },
        Suspended: { label: 'Zablokován', variant: 'danger' },
    },
};

interface StatusBadgeProps {
    status: string | undefined | null;
    /** Use a predefined preset map */
    preset?: StatusPreset;
    /** Or provide a custom map */
    statusMap?: Record<string, StatusEntry>;
    /** Fallback Bootstrap variant when status is not in the map */
    fallbackVariant?: string;
    className?: string;
}

/**
 * Shared StatusBadge — renders a Bootstrap Badge with automatic color and label
 * based on a preset or custom status map.
 *
 * Usage:
 *   <StatusBadge status={user.status} preset="user" />
 *   <StatusBadge status={issue.status} preset="issue" />
 */
export const StatusBadge = ({
    status,
    preset,
    statusMap,
    fallbackVariant = 'secondary',
    className,
}: StatusBadgeProps) => {
    if (!status) return null;

    const map = statusMap ?? (preset ? PRESETS[preset] : {});
    const entry = map[status];
    const variant = entry?.variant ?? fallbackVariant;
    const label = entry?.label ?? status;

    return (
        <Badge bg={variant} className={className}>
            {entry?.icon && <><FontAwesomeIcon icon={entry.icon} />{' '}</>}
            {label}
        </Badge>
    );
};
