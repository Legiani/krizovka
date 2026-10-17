'use client';

import { useState } from 'react';
import { Alert, Button, Form, Modal } from 'react-bootstrap';
import type { Guide, GuideStep, EmergencyContact } from './api';
import { createGuide, updateGuide, publishGuide, archiveGuide } from './api';

export function GuideEditor({
  guide,
  organizationId,
  organizationName,
  onClose,
  onSaved,
}: {
  guide: Guide | null;
  organizationId: string;
  organizationName: string;
  onClose: () => void;
  onSaved: () => void;
}) {
  const initialSteps: GuideStep[] = (() => {
    try {
      return guide?.stepsJson ? JSON.parse(guide.stepsJson) : [];
    } catch {
      return [];
    }
  })();

  const initialContacts: EmergencyContact[] = (() => {
    try {
      return guide?.emergencyContactsJson ? JSON.parse(guide.emergencyContactsJson) : [];
    } catch {
      return [];
    }
  })();

  const [title, setTitle] = useState(guide?.title ?? '');
  const [category, setCategory] = useState(guide?.category ?? 'Bezpečnost a měkké cíle');
  const [summary, setSummary] = useState(guide?.summary ?? '');
  const [quickSummary, setQuickSummary] = useState(guide?.quickSummary ?? '');
  const [diagramMermaid, setDiagramMermaid] = useState(guide?.diagramMermaid ?? '');
  const [steps, setSteps] = useState<GuideStep[]>(initialSteps);
  const [contacts, setContacts] = useState<EmergencyContact[]>(initialContacts);

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const handleAddStep = (isDoNot = false) => {
    const nextNum = steps.length + 1;
    setSteps([
      ...steps,
      {
        stepNumber: nextNum,
        title: isDoNot ? 'ZÁKAZ' : `Krok ${nextNum}`,
        description: '',
        isDoNot,
      },
    ]);
  };

  const handleUpdateStep = (index: number, field: keyof GuideStep, value: string | boolean | number) => {
    const updated = [...steps];
    updated[index] = { ...updated[index], [field]: value };
    setSteps(updated);
  };

  const handleRemoveStep = (index: number) => {
    const updated = steps.filter((_, i) => i !== index);
    // Renumber
    setSteps(updated.map((s, idx) => ({ ...s, stepNumber: idx + 1 })));
  };

  const handleAddContact = () => {
    setContacts([...contacts, { name: '', phone: '', role: 'IZS' }]);
  };

  const handleUpdateContact = (index: number, field: keyof EmergencyContact, value: string) => {
    const updated = [...contacts];
    updated[index] = { ...updated[index], [field]: value };
    setContacts(updated);
  };

  const handleRemoveContact = (index: number) => {
    setContacts(contacts.filter((_, i) => i !== index));
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !category.trim()) {
      setError('Vyplňte název a kategorii metodiky.');
      return;
    }

    setSaving(true);
    setError('');
    try {
      const stepsJson = JSON.stringify(steps);
      const emergencyContactsJson = JSON.stringify(contacts);

      if (guide?.id) {
        await updateGuide(guide.id, {
          title: title.trim(),
          category: category.trim(),
          summary: summary.trim(),
          quickSummary: quickSummary.trim(),
          diagramMermaid: diagramMermaid.trim(),
          stepsJson,
          emergencyContactsJson,
        });
      } else {
        await createGuide({
          organizationId,
          title: title.trim(),
          category: category.trim(),
          summary: summary.trim(),
          quickSummary: quickSummary.trim(),
          diagramMermaid: diagramMermaid.trim(),
          stepsJson,
          emergencyContactsJson,
        });
      }

      onSaved();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  const handlePublish = async () => {
    if (!guide?.id) return;
    setSaving(true);
    try {
      await publishGuide(guide.id);
      onSaved();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  const handleArchive = async () => {
    if (!guide?.id) return;
    setSaving(true);
    try {
      await archiveGuide(guide.id);
      onSaved();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal show onHide={onClose} size="lg" centered aria-labelledby="guide-editor-title">
      <Modal.Header closeButton closeLabel="Zavřít">
        <Modal.Title id="guide-editor-title">
          {guide ? `Upravit metodiku: ${guide.title}` : `Nová krizová metodika (${organizationName})`}
        </Modal.Title>
      </Modal.Header>
      <Form onSubmit={handleSave}>
        <Modal.Body>
          {error && <Alert variant="danger" role="alert">{error}</Alert>}

          <div className="row g-3 mb-3">
            <Form.Group controlId="guide-title" className="col-md-8">
              <Form.Label>Název krizové situace / metodiky</Form.Label>
              <Form.Control
                required
                value={title}
                onChange={e => setTitle(e.target.value)}
                placeholder="např. Aktivní střelec v budově"
              />
            </Form.Group>
            <Form.Group controlId="guide-category" className="col-md-4">
              <Form.Label>Kategorie (Doména)</Form.Label>
              <Form.Select value={category} onChange={e => setCategory(e.target.value)}>
                <option value="Bezpečnost a měkké cíle">Bezpečnost a měkké cíle</option>
                <option value="Přírodní a živelní jevy">Přírodní a živelní jevy</option>
                <option value="Technologie a havárie">Technologie a havárie</option>
                <option value="Zdravotní a hygiena">Zdravotní a hygiena</option>
                <option value="Kybernetika a data">Kybernetika a data</option>
                <option value="Organizace a reputace">Organizace a reputace</option>
              </Form.Select>
            </Form.Group>
            <Form.Group controlId="guide-summary" className="col-12">
              <Form.Label>Anotovaný souhrn (popis metodiky)</Form.Label>
              <Form.Control
                as="textarea"
                rows={2}
                value={summary}
                onChange={e => setSummary(e.target.value)}
                placeholder="Stručný návod, který se zobrazí v přehledu."
              />
            </Form.Group>
            <Form.Group controlId="guide-quick-summary" className="col-12">
              <Form.Label className="text-danger fw-bold">⚡ Bleskové shrnutí pro člověka v tísni (30 vteřin)</Form.Label>
              <Form.Control
                as="textarea"
                rows={2}
                value={quickSummary}
                onChange={e => setQuickSummary(e.target.value)}
                placeholder="1. Co udělat hned. 2. Kam se ukrýt. 3. Co striktně nedělat."
              />
            </Form.Group>
            <Form.Group controlId="guide-diagram" className="col-12">
              <Form.Label className="fw-semibold">📊 Procesní diagram (Mermaid flowchart TD)</Form.Label>
              <Form.Control
                as="textarea"
                rows={3}
                className="font-monospace small"
                value={diagramMermaid}
                onChange={e => setDiagramMermaid(e.target.value)}
                placeholder="flowchart TD&#10;  Start([Start]) --> Decision{Volná cesta?}&#10;  Decision -- ANO --> Run[Uteč]"
              />
            </Form.Group>
          </div>

          <hr className="my-3" />

          {/* Structured steps editor */}
          <div className="d-flex justify-content-between align-items-center mb-2">
            <h6 className="fw-bold mb-0">📝 Prioritní kroky & Zákazy</h6>
            <div className="d-flex gap-2">
              <Button size="sm" variant="outline-primary" onClick={() => handleAddStep(false)}>
                + Přidat krok
              </Button>
              <Button size="sm" variant="outline-danger" onClick={() => handleAddStep(true)}>
                + Přidat zákaz
              </Button>
            </div>
          </div>

          {steps.length === 0 ? (
            <p className="small text-muted mb-3">Zatím nebyly přidány žádné kroky.</p>
          ) : (
            <div className="d-flex flex-column gap-2 mb-3">
              {steps.map((st, idx) => (
                <div
                  key={idx}
                  className={`p-2 border rounded ${st.isDoNot ? 'border-danger bg-danger-subtle' : 'border-secondary-subtle bg-light'}`}
                >
                  <div className="d-flex gap-2 align-items-center mb-2">
                    <span className="badge bg-secondary">#{idx + 1}</span>
                    <Form.Control
                      size="sm"
                      className="fw-bold"
                      value={st.title}
                      placeholder="Heslo (např. UTEČ nebo ZÁKAZ)"
                      onChange={e => handleUpdateStep(idx, 'title', e.target.value)}
                    />
                    <Form.Check
                      type="checkbox"
                      id={`donot-${idx}`}
                      label="Zákaz"
                      checked={st.isDoNot ?? false}
                      onChange={e => handleUpdateStep(idx, 'isDoNot', e.target.checked)}
                    />
                    <Button
                      size="sm"
                      variant="outline-danger"
                      onClick={() => handleRemoveStep(idx)}
                      aria-label="Odstranit krok"
                    >
                      ✕
                    </Button>
                  </div>
                  <Form.Control
                    size="sm"
                    as="textarea"
                    rows={2}
                    value={st.description}
                    placeholder="Podrobný popis postupu..."
                    onChange={e => handleUpdateStep(idx, 'description', e.target.value)}
                  />
                </div>
              ))}
            </div>
          )}

          <hr className="my-3" />

          {/* Emergency contacts editor */}
          <div className="d-flex justify-content-between align-items-center mb-2">
            <h6 className="fw-bold mb-0">📞 Místní tísňové kontakty</h6>
            <Button size="sm" variant="outline-secondary" onClick={handleAddContact}>
              + Přidat kontakt
            </Button>
          </div>

          {contacts.length === 0 ? (
            <p className="small text-muted mb-0">Žádné tísňové kontakty.</p>
          ) : (
            <div className="d-flex flex-column gap-2">
              {contacts.map((c, idx) => (
                <div key={idx} className="d-flex gap-2 align-items-center">
                  <Form.Control
                    size="sm"
                    placeholder="Název (např. Vrátnice)"
                    value={c.name}
                    onChange={e => handleUpdateContact(idx, 'name', e.target.value)}
                  />
                  <Form.Control
                    size="sm"
                    placeholder="Telefon (např. 158)"
                    value={c.phone}
                    onChange={e => handleUpdateContact(idx, 'phone', e.target.value)}
                  />
                  <Form.Control
                    size="sm"
                    placeholder="Role (např. Ostraha)"
                    value={c.role}
                    onChange={e => handleUpdateContact(idx, 'role', e.target.value)}
                  />
                  <Button
                    size="sm"
                    variant="outline-danger"
                    onClick={() => handleRemoveContact(idx)}
                  >
                    ✕
                  </Button>
                </div>
              ))}
            </div>
          )}
        </Modal.Body>
        <Modal.Footer className="d-flex justify-content-between">
          <div>
            {guide?.id && guide.status === 'Draft' && (
              <Button variant="success" size="sm" onClick={handlePublish} disabled={saving}>
                Zveřejnit metodiku
              </Button>
            )}
            {guide?.id && guide.status === 'Published' && (
              <Button variant="outline-warning" size="sm" onClick={handleArchive} disabled={saving}>
                Archivovat
              </Button>
            )}
          </div>
          <div className="d-flex gap-2">
            <Button variant="outline-secondary" onClick={onClose} disabled={saving}>
              Zrušit
            </Button>
            <Button type="submit" variant="primary" disabled={saving}>
              {saving ? 'Ukládám...' : 'Uložit metodiku'}
            </Button>
          </div>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}
