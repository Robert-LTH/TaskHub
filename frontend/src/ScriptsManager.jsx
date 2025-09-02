import { useEffect, useState } from 'react';

export default function ScriptsManager() {
  const [scripts, setScripts] = useState([]);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState({ name: '', description: '', content: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const load = () => {
    setLoading(true);
    fetch('/scripts')
      .then(r => r.json())
      .then(setScripts)
      .catch(() => setScripts([]))
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const resetForm = () => {
    setEditing(null);
    setForm({ name: '', description: '', content: '' });
    setError('');
  };

  const onEdit = (item) => {
    setEditing(item);
    setForm({ name: item.name, description: item.description || '', content: item.content });
  };

  const onDelete = async (item) => {
    if (!confirm(`Delete script "${item.name}"?`)) return;
    await fetch(`/scripts/${item.id}`, { method: 'DELETE' });
    load();
    if (editing && editing.id === item.id) resetForm();
  };

  const onCopyId = async (item) => {
    try {
      await navigator.clipboard.writeText(item.id);
      alert('Script id copied');
    } catch {
      const ta = document.createElement('textarea');
      ta.value = item.id;
      document.body.appendChild(ta);
      ta.select();
      document.execCommand('copy');
      document.body.removeChild(ta);
      alert('Script id copied');
    }
  };

  const onRun = async (item) => {
    const body = {
      commands: ['powershell-script'],
      payload: { scriptId: item.id },
      delay: null,
      signature: null
    };
    const res = await fetch('/commands', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
    if (!res.ok) { alert('Failed to enqueue command'); return; }
    const data = await res.json();
    alert(`Enqueued job ${data.jobId || data.id || 'unknown'}`);
  };

  const onSubmit = async (e) => {
    e.preventDefault();
    setError('');
    if (!form.name.trim()) { setError('Name is required'); return; }
    const payload = { id: editing?.id || '', name: form.name, description: form.description, content: form.content };
    const wrapped = { payload, signature: null };
    const method = editing ? 'PUT' : 'POST';
    const url = editing ? `/scripts/${editing.id}` : '/scripts';
    const res = await fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(wrapped) });
    if (!res.ok) { setError('Save failed'); return; }
    load();
    resetForm();
  };

  return (
    <div style={{ display: 'flex', height: '100%', gap: 16 }}>
      <div style={{ width: 360, borderRight: '1px solid #ccc', paddingRight: 12 }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <h3>Scripts</h3>
          <button onClick={load} disabled={loading}>{loading ? 'Loading…' : 'Refresh'}</button>
        </div>
        <div style={{ overflowY: 'auto', maxHeight: 'calc(100vh - 140px)' }}>
          {scripts.map(s => (
            <div key={s.id} style={{ border: '1px solid #ddd', borderRadius: 4, padding: 8, marginBottom: 8 }}>
              <div style={{ fontWeight: 600 }}>{s.name}</div>
              <div style={{ display: 'flex', gap: 6, alignItems: 'center', marginTop: 4 }}>
                <span style={{ fontSize: 12, color: s.isVerified ? '#2e7d32' : '#b00020' }}>
                  {s.isVerified ? 'Signature verified' : 'Unsigned'}
                </span>
              </div>
              {s.description && <div style={{ color: '#666', fontSize: 12 }}>{s.description}</div>}
              <div style={{ marginTop: 8, display: 'flex', gap: 8 }}>
                <button onClick={() => onEdit(s)}>Edit</button>
                <button onClick={() => onRun(s)}>Run Script</button>
                <button onClick={() => onCopyId(s)}>Copy Id</button>
                <button onClick={() => onDelete(s)} style={{ color: '#b00020' }}>Delete</button>
              </div>
              <div style={{ marginTop: 8, fontSize: 12, color: '#666' }}>Id: {s.id}</div>
            </div>
          ))}
          {scripts.length === 0 && !loading && <div>No scripts yet</div>}
        </div>
      </div>
      <div style={{ flex: 1 }}>
        <h3>{editing ? 'Edit Script' : 'New Script'}</h3>
        <form onSubmit={onSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          <label>
            <div>Name</div>
            <input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} style={{ width: '100%' }} />
          </label>
          <label>
            <div>Description</div>
            <input value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} style={{ width: '100%' }} />
          </label>
          <label>
            <div>Content (PowerShell)</div>
            <textarea value={form.content} onChange={e => setForm({ ...form, content: e.target.value })} rows={18} style={{ width: '100%', fontFamily: 'monospace' }} />
          </label>
          {error && <div style={{ color: '#b00020' }}>{error}</div>}
          <div style={{ display: 'flex', gap: 8 }}>
            <button type="submit">{editing ? 'Save' : 'Create'}</button>
            {editing && <button type="button" onClick={resetForm}>Cancel</button>}
          </div>
          <div style={{ marginTop: 8, color: '#666', fontSize: 12 }}>
            Tip: Use in a command payload with a "scriptId" field.
          </div>
        </form>
      </div>
    </div>
  );
}
