'use client';

import { useState, useEffect, useCallback, useMemo } from 'react';
import { Alert, Badge, Button, Form, Modal, Nav, Spinner, Tab, Tabs } from 'react-bootstrap';
import { ModulePage } from '@/shared/components/ModulePage';
import { StatusBadge } from '@/shared/components/StatusBadge';
import { useDemo } from '@/demo/store';
import {
  fetchGuides,
  fetchGuideTemplates,
  cloneGuideFromTemplate,
  batchCloneGuidesFromTemplates,
  type Guide,
  type GuideTemplate,
  type GuideStep,
  type EmergencyContact,
  type LegalBasisItem,
} from '@/features/guides/api';
import { GuideEditor } from '@/features/guides/GuideEditor';

const DOMAINS = [
  'Všechny',
  'Bezpečnost a měkké cíle',
  'Přírodní a živelní jevy',
  'Technologie a havárie',
  'Zdravotní a hygiena',
  'Kybernetika a data',
  'Organizace a reputace',
];

const PRESETS = [
  {
    key: 'School',
    title: '🎒 Balíček ŠKOLA (VŠ/SŠ/ZŠ)',
    description: '40 šablon: AMOK, NVS, zbraň, požár, KPR/AED, šikana/obtěžování, neoprávněný vstup, majetek, sjednocení dětí a rodičů.',
    color: 'primary',
  },
  {
    key: 'Municipality',
    title: '🏛️ Balíček OBEC / ÚŘAD',
    description: '39 šablon: Povodně, orkán, blackout, výpadek vody/plynu, demonstrace, krizové řízení, neoprávněný vstup.',
    color: 'info',
  },
  {
    key: 'Enterprise',
    title: '🏭 Balíček PODNIK / PRŮMYSL',
    description: '39 šablon: SEVESO chemie, PO, pracovní úraz START, ransomware NIS 2, GDPR únik, whistleblowing, ochrana majetku.',
    color: 'dark',
  },
];

export default function GuidesPage() {
  const { activeOrganization } = useDemo();
  const [activeTab, setActiveTab] = useState<'org' | 'templates'>('org');
  const [categoryFilter, setCategoryFilter] = useState('Všechny');
  const [searchQuery, setSearchQuery] = useState('');

  const [guides, setGuides] = useState<Guide[]>([]);
  const [templates, setTemplates] = useState<GuideTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [notice, setNotice] = useState('');
  const [errorNotice, setErrorNotice] = useState('');

  const [selectedGuide, setSelectedGuide] = useState<Guide | null>(null);
  const [selectedTemplate, setSelectedTemplate] = useState<GuideTemplate | null>(null);
  const [editorGuide, setEditorGuide] = useState<Guide | 'new' | null>(null);
  const [cloningId, setCloningId] = useState<string | null>(null);
  const [batchCloning, setBatchCloning] = useState(false);

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      setErrorNotice('');
      const [guidesData, templatesData] = await Promise.all([
        fetchGuides({ organizationId: activeOrganization.id }),
        fetchGuideTemplates(),
      ]);
      setGuides(guidesData);
      setTemplates(templatesData);
    } catch (err) {
      setErrorNotice((err as Error).message);
    } finally {
      setLoading(false);
    }
  }, [activeOrganization.id]);

  useEffect(() => {
    const controller = new AbortController();
    Promise.resolve().then(() => {
      loadData();
    });
    return () => controller.abort();
  }, [loadData]);

  const handleClone = async (templateId: string) => {
    setCloningId(templateId);
    setErrorNotice('');
    try {
      const cloned = await cloneGuideFromTemplate(templateId, activeOrganization.id);
      setNotice(`Šablona byla úspěšně převzata jako koncept pro ${activeOrganization.name}.`);
      setActiveTab('org');
      await loadData();
      setEditorGuide(cloned);
    } catch (err) {
      setErrorNotice((err as Error).message);
    } finally {
      setCloningId(null);
    }
  };

  const handleBatchPreset = async (presetKey: string) => {
    setBatchCloning(true);
    setErrorNotice('');
    try {
      const matchingTemplates = templates.filter(t => {
        try {
          const types: string[] = t.recommendedOrgTypesJson ? JSON.parse(t.recommendedOrgTypesJson) : [];
          return types.includes(presetKey);
        } catch {
          return false;
        }
      });

      if (matchingTemplates.length === 0) {
        throw new Error('Nebyly nalezeny žádné šablony pro tento balíček.');
      }

      const ids = matchingTemplates.map(t => t.id);
      await batchCloneGuidesFromTemplates(ids, activeOrganization.id);
      setNotice(`Úspěšně byl převzat celý balíček (${matchingTemplates.length} metodik) pro ${activeOrganization.name}.`);
      setActiveTab('org');
      await loadData();
    } catch (err) {
      setErrorNotice((err as Error).message);
    } finally {
      setBatchCloning(false);
    }
  };

  const parseSteps = (json?: string): GuideStep[] => {
    try {
      return json ? JSON.parse(json) : [];
    } catch {
      return [];
    }
  };

  const parseContacts = (json?: string): EmergencyContact[] => {
    try {
      return json ? JSON.parse(json) : [];
    } catch {
      return [];
    }
  };

  const parseLegal = (json?: string): LegalBasisItem[] => {
    try {
      return json ? JSON.parse(json) : [];
    } catch {
      return [];
    }
  };

  const filteredGuides = useMemo(() => {
    return guides.filter(g => {
      const matchesCategory = categoryFilter === 'Všechny' || g.category === categoryFilter;
      const matchesSearch =
        !searchQuery ||
        g.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        g.summary.toLowerCase().includes(searchQuery.toLowerCase()) ||
        (g.quickSummary && g.quickSummary.toLowerCase().includes(searchQuery.toLowerCase()));
      return matchesCategory && matchesSearch;
    });
  }, [guides, categoryFilter, searchQuery]);

  const filteredTemplates = useMemo(() => {
    return templates.filter(t => {
      const matchesCategory = categoryFilter === 'Všechny' || t.category === categoryFilter;
      const matchesSearch =
        !searchQuery ||
        t.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        t.summary.toLowerCase().includes(searchQuery.toLowerCase()) ||
        (t.quickSummary && t.quickSummary.toLowerCase().includes(searchQuery.toLowerCase()));
      return matchesCategory && matchesSearch;
    });
  }, [templates, categoryFilter, searchQuery]);

  return (
    <>
      <ModulePage
        title="Krizové metodiky"
        subtitle={`${activeOrganization.name} · manuály pro zaměstnance a studenty`}
        noCard
        actions={
          <div className="d-flex gap-2">
            <Button
              variant={activeTab === 'templates' ? 'outline-primary' : 'primary'}
              onClick={() => {
                setNotice('');
                setEditorGuide('new');
              }}
            >
              + Nová metodika od nuly
            </Button>
          </div>
        }
      >
        <Nav variant="pills" className="mb-4" activeKey={activeTab}>
          <Nav.Item>
            <Nav.Link
              eventKey="org"
              active={activeTab === 'org'}
              onClick={() => setActiveTab('org')}
            >
              Metodiky organizace ({guides.length})
            </Nav.Link>
          </Nav.Item>
          <Nav.Item>
            <Nav.Link
              eventKey="templates"
              active={activeTab === 'templates'}
              onClick={() => setActiveTab('templates')}
            >
              Centrální knihovna šablon ({templates.length})
            </Nav.Link>
          </Nav.Item>
        </Nav>

        {notice && <Alert variant="success" role="status" dismissible onClose={() => setNotice('')}>{notice}</Alert>}
        {errorNotice && <Alert variant="danger" role="alert" dismissible onClose={() => setErrorNotice('')}>{errorNotice}</Alert>}

        {/* Preset Packages Banner in Templates Tab */}
        {activeTab === 'templates' && (
          <div className="card mb-4 border-primary-subtle bg-light shadow-sm">
            <div className="card-body">
              <div className="d-flex justify-content-between align-items-center mb-2">
                <h6 className="fw-bold mb-0 text-primary">⚡ Rychlé balíčky šablon pro 1-click převzetí</h6>
                {batchCloning && <Spinner size="sm" animation="border" variant="primary" />}
              </div>
              <p className="small text-secondary mb-3">
                Nemusíte vybírat metodiky jednotlivě. Zvolte profil vaší instituce a převezměte celou sadu krizových postupů na jedno kliknutí.
              </p>
              <div className="row g-2">
                {PRESETS.map(p => (
                  <div key={p.key} className="col-md-4">
                    <div className="border rounded p-3 h-100 bg-white d-flex flex-column justify-content-between shadow-xs">
                      <div>
                        <div className="fw-bold mb-1">{p.title}</div>
                        <div className="small text-muted mb-3">{p.description}</div>
                      </div>
                      <Button
                        size="sm"
                        variant={`outline-${p.color}`}
                        disabled={batchCloning}
                        onClick={() => handleBatchPreset(p.key)}
                      >
                        Převzít balíček do {activeOrganization.name}
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* Category Filters and Search Bar */}
        <div className="d-flex flex-wrap gap-2 justify-content-between align-items-center mb-4">
          <div className="d-flex flex-wrap gap-1">
            {DOMAINS.map(dom => (
              <Button
                key={dom}
                size="sm"
                variant={categoryFilter === dom ? 'primary' : 'outline-secondary'}
                onClick={() => setCategoryFilter(dom)}
                className="rounded-pill px-3"
              >
                {dom}
              </Button>
            ))}
          </div>
          <div style={{ maxWidth: 280 }} className="w-100">
            <Form.Control
              size="sm"
              type="search"
              placeholder="Hledat (střelec, požár, povodeň)..."
              value={searchQuery}
              onChange={e => setSearchQuery(e.target.value)}
            />
          </div>
        </div>

        {loading ? (
          <div className="text-center py-5">
            <Spinner animation="border" role="status" variant="primary" />
            <p className="mt-2 text-secondary">Načítám data metodik z API...</p>
          </div>
        ) : activeTab === 'org' ? (
          /* Organization Guides */
          filteredGuides.length === 0 ? (
            <div className="text-center py-5 border rounded bg-light">
              <h5 className="text-muted">Organizace nemá žádné odpovídající krizové metodiky</h5>
              <p className="small text-secondary mb-3">
                Můžete vytvořit novou metodiku od nuly nebo převzít standardizovaný postup z centrální knihovny šablon.
              </p>
              <Button variant="outline-primary" onClick={() => setActiveTab('templates')}>
                Přejít do knihovny šablon
              </Button>
            </div>
          ) : (
            <div className="guide-grid">
              {filteredGuides.map(guide => {
                const steps = parseSteps(guide.stepsJson);
                return (
                  <article className="guide-card" key={guide.id}>
                    <div className="d-flex justify-content-between align-items-center mb-2">
                      <StatusBadge status={guide.status} preset="article" />
                      <span className="badge bg-light text-dark border">{guide.category}</span>
                    </div>
                    <h2>{guide.title}</h2>
                    <p className="text-secondary small mb-2">{guide.summary}</p>

                    {guide.quickSummary && (
                      <div className="p-2 border-start border-3 border-danger bg-danger-subtle rounded small mb-3">
                        <strong className="text-danger d-block">⚡ 30s krizové shrnutí:</strong>
                        <span className="text-dark">{guide.quickSummary}</span>
                      </div>
                    )}

                    <div className="small text-muted mb-3">
                      {steps.length} {steps.length === 1 ? 'krok' : steps.length < 5 ? 'kroky' : 'kroků'}
                      {guide.sourceTemplateId && (
                        <span> · šablona: <code>{guide.sourceTemplateId}</code></span>
                      )}
                    </div>
                    <div className="d-flex gap-2 mt-auto">
                      <Button
                        size="sm"
                        variant="outline-secondary"
                        onClick={() => setSelectedGuide(guide)}
                      >
                        Zobrazit detail
                      </Button>
                      <Button
                        size="sm"
                        variant="outline-primary"
                        onClick={() => {
                          setNotice('');
                          setEditorGuide(guide);
                        }}
                      >
                        Upravit kroky
                      </Button>
                    </div>
                  </article>
                );
              })}
            </div>
          )
        ) : (
          /* Central Templates Library */
          filteredTemplates.length === 0 ? (
            <div className="text-center py-5 border rounded bg-light">
              <h5 className="text-muted">Nebyly nalezeny žádné šablony odpovídající filtru</h5>
            </div>
          ) : (
            <div className="guide-grid">
              {filteredTemplates.map(tmpl => {
                const steps = parseSteps(tmpl.stepsJson);
                const isCloning = cloningId === tmpl.id;
                const alreadyCloned = guides.some(g => g.sourceTemplateId === tmpl.id);
                return (
                  <article className="guide-card" key={tmpl.id}>
                    <div className="d-flex justify-content-between align-items-center mb-2">
                      <Badge bg="info" className="fw-normal">Šablona v{tmpl.version}</Badge>
                      <span className="badge bg-light text-dark border">{tmpl.category}</span>
                    </div>
                    <h2>{tmpl.title}</h2>
                    <p className="text-secondary small mb-2">{tmpl.summary}</p>

                    {tmpl.quickSummary && (
                      <div className="p-2 border-start border-3 border-warning bg-warning-subtle rounded small mb-3">
                        <strong className="text-warning-emphasis d-block">⚡ 30s rychlé shrnutí:</strong>
                        <span className="text-dark">{tmpl.quickSummary}</span>
                      </div>
                    )}

                    <p className="small text-muted mb-3">
                      Obsahuje {steps.length} kroků · diagram toku · právní odkazy.
                    </p>
                    <div className="d-flex gap-2 mt-auto">
                      <Button
                        size="sm"
                        variant="outline-secondary"
                        onClick={() => setSelectedTemplate(tmpl)}
                      >
                        Prohlédnout
                      </Button>
                      <Button
                        size="sm"
                        variant={alreadyCloned ? 'outline-success' : 'success'}
                        onClick={() => handleClone(tmpl.id)}
                        disabled={isCloning}
                      >
                        {isCloning ? 'Převádím...' : alreadyCloned ? 'Převzít další variantu' : 'Převzít do organizace'}
                      </Button>
                    </div>
                  </article>
                );
              })}
            </div>
          )
        )}
      </ModulePage>

      {/* Guide Detail Modal */}
      {selectedGuide && (
        <Modal show onHide={() => setSelectedGuide(null)} size="lg" centered>
          <Modal.Header closeButton closeLabel="Zavřít">
            <Modal.Title>{selectedGuide.title}</Modal.Title>
          </Modal.Header>
          <Modal.Body>
            <div className="d-flex gap-2 mb-3">
              <StatusBadge status={selectedGuide.status} preset="article" />
              <Badge bg="secondary">{selectedGuide.category}</Badge>
            </div>

            {selectedGuide.quickSummary && (
              <div className="alert alert-danger border-start border-4 border-danger shadow-xs mb-4">
                <h6 className="alert-heading fw-bold d-flex align-items-center gap-2 mb-1">
                  <span>⚡</span> 30-VTEŘINOVÝ PŘEHLED PRO ČLOVĚKA V TÍSNI
                </h6>
                <div className="fs-6">{selectedGuide.quickSummary}</div>
              </div>
            )}

            <p className="lead fs-6 mb-4">{selectedGuide.summary}</p>

            <Tabs defaultActiveKey="steps" id="guide-detail-tabs" className="mb-3">
              <Tab eventKey="steps" title={`Kroky a zákazy (${parseSteps(selectedGuide.stepsJson).length})`}>
                <div className="d-flex flex-column gap-2 mb-3">
                  {parseSteps(selectedGuide.stepsJson).map((step, idx) => (
                    <div
                      key={idx}
                      className={`p-3 border rounded ${step.isDoNot ? 'border-danger bg-danger-subtle' : 'border-secondary-subtle bg-light'}`}
                    >
                      <div className="fw-bold mb-1">
                        {step.stepNumber}. {step.title}
                      </div>
                      <div className="small">{step.description}</div>
                    </div>
                  ))}
                </div>
              </Tab>

              {selectedGuide.diagramMermaid && (
                <Tab eventKey="diagram" title="📊 Procesní diagram">
                  <div className="card bg-light border p-3 mb-3">
                    <h6 className="fw-bold text-primary mb-2">Rozhodovací strom / Tok reakce</h6>
                    <pre className="bg-dark text-light p-3 rounded small overflow-auto mb-0 font-monospace">
                      {selectedGuide.diagramMermaid}
                    </pre>
                  </div>
                </Tab>
              )}

              {selectedGuide.legalBasisJson && (
                <Tab eventKey="legal" title="📜 Legislativa a normy">
                  <div className="d-flex flex-column gap-2 mb-3">
                    {parseLegal(selectedGuide.legalBasisJson).map((l, idx) => (
                      <div key={idx} className="p-3 border rounded bg-light">
                        <div className="fw-bold text-dark">{l.title}</div>
                        <div className="small text-muted mb-1">
                          Předpis: <strong>{l.lawNumber}</strong> · Paragraf: <code>{l.section}</code> · Vydal: {l.issuer}
                        </div>
                        {l.url && (
                          <a href={l.url} target="_blank" rel="noopener noreferrer" className="small fw-semibold text-primary">
                            Otevřít plné znění předpisu ↗
                          </a>
                        )}
                      </div>
                    ))}
                  </div>
                </Tab>
              )}

              <Tab eventKey="contacts" title={`Tísňové kontakty (${parseContacts(selectedGuide.emergencyContactsJson).length})`}>
                <div className="d-flex flex-wrap gap-2 mb-3">
                  {parseContacts(selectedGuide.emergencyContactsJson).map((c, idx) => (
                    <div key={idx} className="badge bg-light text-dark border p-2 fs-6">
                      <span className="fw-bold">{c.name}:</span> <code>{c.phone}</code> ({c.role})
                    </div>
                  ))}
                </div>
              </Tab>
            </Tabs>
          </Modal.Body>
          <Modal.Footer>
            <Button variant="outline-secondary" onClick={() => setSelectedGuide(null)}>
              Zavřít
            </Button>
          </Modal.Footer>
        </Modal>
      )}

      {/* Template Detail Modal */}
      {selectedTemplate && (
        <Modal show onHide={() => setSelectedTemplate(null)} size="lg" centered>
          <Modal.Header closeButton closeLabel="Zavřít">
            <Modal.Title>Šablona: {selectedTemplate.title}</Modal.Title>
          </Modal.Header>
          <Modal.Body>
            <div className="d-flex gap-2 mb-3">
              <Badge bg="info">Verze {selectedTemplate.version}</Badge>
              <Badge bg="secondary">{selectedTemplate.category}</Badge>
            </div>

            {selectedTemplate.quickSummary && (
              <div className="alert alert-warning border-start border-4 border-warning shadow-xs mb-4">
                <h6 className="alert-heading fw-bold d-flex align-items-center gap-2 mb-1">
                  <span>⚡</span> 30-VTEŘINOVÝ PŘEHLED PRO ČLOVĚKA V TÍSNI
                </h6>
                <div className="fs-6">{selectedTemplate.quickSummary}</div>
              </div>
            )}

            <p className="lead fs-6 mb-4">{selectedTemplate.summary}</p>

            <Tabs defaultActiveKey="steps" id="template-detail-tabs" className="mb-3">
              <Tab eventKey="steps" title={`Kroky a zákazy (${parseSteps(selectedTemplate.stepsJson).length})`}>
                <div className="d-flex flex-column gap-2 mb-3">
                  {parseSteps(selectedTemplate.stepsJson).map((step, idx) => (
                    <div
                      key={idx}
                      className={`p-3 border rounded ${step.isDoNot ? 'border-danger bg-danger-subtle' : 'border-secondary-subtle bg-light'}`}
                    >
                      <div className="fw-bold mb-1">
                        {step.stepNumber}. {step.title}
                      </div>
                      <div className="small">{step.description}</div>
                    </div>
                  ))}
                </div>
              </Tab>

              {selectedTemplate.diagramMermaid && (
                <Tab eventKey="diagram" title="📊 Procesní diagram">
                  <div className="card bg-light border p-3 mb-3">
                    <h6 className="fw-bold text-primary mb-2">Rozhodovací strom / Mermaid flowchart</h6>
                    <pre className="bg-dark text-light p-3 rounded small overflow-auto mb-0 font-monospace">
                      {selectedTemplate.diagramMermaid}
                    </pre>
                  </div>
                </Tab>
              )}

              {selectedTemplate.legalBasisJson && (
                <Tab eventKey="legal" title="📜 Legislativa a normy">
                  <div className="d-flex flex-column gap-2 mb-3">
                    {parseLegal(selectedTemplate.legalBasisJson).map((l, idx) => (
                      <div key={idx} className="p-3 border rounded bg-light">
                        <div className="fw-bold text-dark">{l.title}</div>
                        <div className="small text-muted mb-1">
                          Předpis: <strong>{l.lawNumber}</strong> · Paragraf: <code>{l.section}</code> · Vydal: {l.issuer}
                        </div>
                        {l.url && (
                          <a href={l.url} target="_blank" rel="noopener noreferrer" className="small fw-semibold text-primary">
                            Otevřít plné znění předpisu ↗
                          </a>
                        )}
                      </div>
                    ))}
                  </div>
                </Tab>
              )}

              <Tab eventKey="contacts" title={`Výchozí kontakty (${parseContacts(selectedTemplate.emergencyContactsJson).length})`}>
                <div className="d-flex flex-wrap gap-2 mb-3">
                  {parseContacts(selectedTemplate.emergencyContactsJson).map((c, idx) => (
                    <div key={idx} className="badge bg-light text-dark border p-2 fs-6">
                      <span className="fw-bold">{c.name}:</span> <code>{c.phone}</code> ({c.role})
                    </div>
                  ))}
                </div>
              </Tab>
            </Tabs>
          </Modal.Body>
          <Modal.Footer className="d-flex justify-content-between">
            <Button variant="outline-secondary" onClick={() => setSelectedTemplate(null)}>
              Zavřít
            </Button>
            <Button
              variant="success"
              onClick={() => {
                const id = selectedTemplate.id;
                setSelectedTemplate(null);
                handleClone(id);
              }}
            >
              Převzít do {activeOrganization.name}
            </Button>
          </Modal.Footer>
        </Modal>
      )}

      {/* Guide Editor */}
      {editorGuide && (
        <GuideEditor
          key={editorGuide === 'new' ? 'new' : editorGuide.id}
          guide={editorGuide === 'new' ? null : editorGuide}
          organizationId={activeOrganization.id}
          organizationName={activeOrganization.name}
          onClose={() => setEditorGuide(null)}
          onSaved={() => {
            setEditorGuide(null);
            setNotice('Metodika byla úspěšně uložena.');
            loadData();
          }}
        />
      )}
    </>
  );
}
