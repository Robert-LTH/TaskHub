import { useCallback, useEffect, useRef, useState } from 'react';
import ReactFlow, {
  ReactFlowProvider,
  addEdge,
  Background,
  Controls
} from 'reactflow';
import 'reactflow/dist/style.css';

import CommandNode from './CommandNode';

export default function App() {
  const [commands, setCommands] = useState([]);
  const [nodes, setNodes] = useState([]);
  const [edges, setEdges] = useState([]);
  const [reactFlowInstance, setReactFlowInstance] = useState(null);
  const id = useRef(0);
  const reactFlowWrapper = useRef(null);

  useEffect(() => {
    fetch('/commands/available')
      .then(res => res.json())
      .then(setCommands)
      .catch(() => setCommands([]));
  }, []);

  const nodeTypes = { command: CommandNode };

  const onConnect = useCallback(
    (params) => setEdges((eds) => addEdge({ ...params, label: params.sourceHandle }, eds)),
    []
  );

  const onDragOver = (event) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
  };

  const onDrop = useCallback(
    (event) => {
      event.preventDefault();
      const commandData = event.dataTransfer.getData('application/reactflow');
      if (!commandData) {
        return;
      }

      const cmd = JSON.parse(commandData);
      const bounds = reactFlowWrapper.current.getBoundingClientRect();
      const position = reactFlowInstance.project({
        x: event.clientX - bounds.left,
        y: event.clientY - bounds.top
      });

      const newNode = {
        id: `${id.current++}`,
        type: 'command',
        position,
        data: { label: cmd.name, inputs: cmd.inputs || [] }
      };

      setNodes((nds) => nds.concat(newNode));
    },
    [reactFlowInstance]
  );

  const onDragStart = (event, cmd) => {
    event.dataTransfer.setData('application/reactflow', JSON.stringify(cmd));
    event.dataTransfer.effectAllowed = 'move';
  };

  return (
    <ReactFlowProvider>
      <div style={{ display: 'flex', height: '100vh' }}>
        <div style={{ width: 200, borderRight: '1px solid #ccc', padding: 10 }}>
          <h3>Commands</h3>
          {commands.map(cmd => (
            <div
              key={cmd.name}
              draggable
              onDragStart={(e) => onDragStart(e, cmd)}
              style={{ padding: 4, border: '1px solid #999', borderRadius: 4, marginBottom: 8, cursor: 'grab' }}
            >
              {cmd.name}
            </div>
          ))}
        </div>
        <div style={{ flex: 1 }} ref={reactFlowWrapper}>
          <ReactFlow
            nodes={nodes}
            edges={edges}
            onConnect={onConnect}
            onInit={setReactFlowInstance}
            onDrop={onDrop}
            onDragOver={onDragOver}
            nodeTypes={nodeTypes}
          >
            <Controls />
            <Background />
          </ReactFlow>
        </div>
      </div>
    </ReactFlowProvider>
  );
}

