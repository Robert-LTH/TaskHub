import { memo } from 'react';
import { Handle, Position } from 'reactflow';

export default memo(function CommandNode({ data }) {
  const inputs = data.inputs || [];
  return (
    <div style={{ padding: 10, border: '1px solid #777', borderRadius: 4, background: '#fff' }}>
      <strong>{data.label}</strong>
      {inputs.map((input, idx) => (
        <Handle
          key={input.name}
          type="target"
          position={Position.Left}
          id={input.name}
          style={{ top: 30 + idx * 20 }}
        />
      ))}
      <Handle
        type="source"
        position={Position.Right}
        id="success"
        style={{ top: 20, background: '#4caf50' }}
      />
      <Handle
        type="source"
        position={Position.Right}
        id="failure"
        style={{ bottom: 20, background: '#f44336' }}
      />
    </div>
  );
});

