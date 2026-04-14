/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'motion/react';
import { 
  Flame, 
  Scroll, 
  Key, 
  Scissors, 
  CheckCircle2, 
  ArrowLeft,
  Loader2,
  Shield,
  Sword,
  Skull
} from 'lucide-react';

// --- Types ---
type TaskType = 'tapestry' | 'runelock' | 'forge' | 'scroll' | null;

// --- Components ---

const TaskPanel = ({ children, title, onBack }: { children: React.ReactNode, title: string, onBack: () => void }) => (
  <motion.div 
    initial={{ opacity: 0, scale: 0.8, rotateX: 20 }}
    animate={{ opacity: 1, scale: 1, rotateX: 0 }}
    exit={{ opacity: 0, scale: 0.8, rotateX: -20 }}
    className="relative w-full max-w-md bg-[#2d241e] border-8 border-[#4a3b2f] rounded-lg p-6 shadow-[0_20px_50px_rgba(0,0,0,0.8)] overflow-hidden"
    style={{ 
      perspective: '1000px',
      backgroundImage: 'url("https://www.transparenttextures.com/patterns/dark-leather.png")'
    }}
  >
    {/* Decorative corners */}
    <div className="absolute top-0 left-0 w-12 h-12 border-t-4 border-l-4 border-amber-600/30 rounded-tl-lg" />
    <div className="absolute top-0 right-0 w-12 h-12 border-t-4 border-r-4 border-amber-600/30 rounded-tr-lg" />
    <div className="absolute bottom-0 left-0 w-12 h-12 border-b-4 border-l-4 border-amber-600/30 rounded-bl-lg" />
    <div className="absolute bottom-0 right-0 w-12 h-12 border-b-4 border-r-4 border-amber-600/30 rounded-br-lg" />

    <div className="flex items-center justify-between mb-6 border-b-2 border-amber-900/50 pb-4">
      <button 
        onClick={onBack}
        className="p-2 hover:bg-amber-900/20 rounded-lg transition-colors text-amber-600"
      >
        <ArrowLeft size={24} />
      </button>
      <h2 className="text-2xl font-medieval font-bold text-amber-100 uppercase tracking-widest drop-shadow-md">{title}</h2>
      <div className="w-10" /> {/* Spacer */}
    </div>
    {children}
  </motion.div>
);

// 1. Tapestry Task (Mending Threads)
type Difficulty = 'facil' | 'avanzado' | 'dificil' | null;

const TapestryTask = ({ onComplete }: { onComplete: () => void }) => {
  const [difficulty, setDifficulty] = useState<Difficulty>(null);
  const [completedLevels, setCompletedLevels] = useState(0);
  const colors = ['#991b1b', '#1e40af', '#854d0e', '#6b21a8'];

  const getRandomDifficulty = (): Difficulty => {
    const diffs: Difficulty[] = ['facil', 'avanzado', 'dificil'];
    return diffs[Math.floor(Math.random() * diffs.length)];
  };

  // Auto-start first level
  useEffect(() => {
    if (!difficulty) {
      setDifficulty(getRandomDifficulty());
    }
  }, []);
  
  const getDifficultyConfig = (diff: Difficulty) => {
    switch (diff) {
      case 'facil':
        return { numColumns: 2, trailPoints: 14, lerpSpeed: 0.45, middleLerp: 0.22 };
      case 'avanzado':
        return { numColumns: 3, trailPoints: 12, lerpSpeed: 0.4, middleLerp: 0.2 };
      case 'dificil':
        return { numColumns: 4, trailPoints: 10, lerpSpeed: 0.35, middleLerp: 0.18 };
      default:
        return { numColumns: 2, trailPoints: 12, lerpSpeed: 0.4, middleLerp: 0.2 };
    }
  };

  const config = getDifficultyConfig(difficulty);
  const [columns, setColumns] = useState<{ id: number, color: string, connectedLeft: boolean, connectedRight: boolean }[][]>([]);
  const [selectedSpool, setSelectedSpool] = useState<{ col: number, idx: number } | null>(null);
  const [connections, setConnections] = useState<{phase: number, from: number, to: number, color: string}[]>([]);
  const containerRef = React.useRef<HTMLDivElement>(null);
  const spoolRefs = React.useRef<(HTMLButtonElement | null)[][]>([]);
  
  // Initialize columns when difficulty is selected or level changes
  useEffect(() => {
    if (difficulty) {
      const newColumns = [];
      spoolRefs.current = [];
      for (let i = 0; i < config.numColumns; i++) {
        const shuffled = [...colors].sort(() => Math.random() - 0.5);
        newColumns.push(shuffled.map((c, idx) => ({
          id: idx,
          color: c,
          connectedLeft: false,
          connectedRight: false
        })));
        spoolRefs.current.push(new Array(shuffled.length).fill(null));
      }
      setColumns(newColumns);
      setConnections([]);
      setSelectedSpool(null);
    }
  }, [difficulty, completedLevels]);

  // Snake Physics State
  const [trail, setTrail] = useState<{x: number, y: number}[]>([]);
  const [mousePos, setMousePos] = useState({ x: 0, y: 0 });
  const [spoolPos, setSpoolPos] = useState({ x: 0, y: 0 });

  // Audio State
  const audioCtxRef = React.useRef<AudioContext | null>(null);
  const noiseNodeRef = React.useRef<AudioBufferSourceNode | null>(null);
  const gainNodeRef = React.useRef<GainNode | null>(null);
  const filterNodeRef = React.useRef<BiquadFilterNode | null>(null);

  const initAudio = () => {
    if (audioCtxRef.current) return;
    const ctx = new (window.AudioContext || (window as any).webkitAudioContext)();
    audioCtxRef.current = ctx;

    const bufferSize = 2 * ctx.sampleRate;
    const noiseBuffer = ctx.createBuffer(1, bufferSize, ctx.sampleRate);
    const output = noiseBuffer.getChannelData(0);
    for (let i = 0; i < bufferSize; i++) {
      const white = Math.random() * 2 - 1;
      output[i] = white * (0.8 + 0.2 * Math.sin(i * 0.05));
    }

    const noise = ctx.createBufferSource();
    noise.buffer = noiseBuffer;
    noise.loop = true;

    const filter = ctx.createBiquadFilter();
    filter.type = 'lowpass';
    filter.frequency.value = 800;
    filter.Q.value = 1;

    const gain = ctx.createGain();
    gain.gain.value = 0;

    noise.connect(filter);
    filter.connect(gain);
    gain.connect(ctx.destination);

    noise.start();
    noiseNodeRef.current = noise;
    gainNodeRef.current = gain;
    filterNodeRef.current = filter;
  };

  useEffect(() => {
    return () => {
      if (audioCtxRef.current) {
        audioCtxRef.current.close();
      }
    };
  }, []);

  useEffect(() => {
    if (selectedSpool === null || !difficulty) return;

    let frameId: number;
    const updateTrail = () => {
      setTrail(prev => {
        if (prev.length === 0) return prev;
        const newTrail = [...prev];
        const n = newTrail.length;
        
        newTrail[n - 1] = {
          x: newTrail[n - 1].x + (mousePos.x - newTrail[n - 1].x) * config.lerpSpeed,
          y: newTrail[n - 1].y + (mousePos.y - newTrail[n - 1].y) * config.lerpSpeed
        };

        newTrail[0] = {
          x: newTrail[0].x + (spoolPos.x - newTrail[0].x) * config.lerpSpeed,
          y: newTrail[0].y + (spoolPos.y - newTrail[0].y) * config.lerpSpeed
        };

        for (let i = 1; i < n - 1; i++) {
          const prevPoint = newTrail[i - 1];
          const nextPoint = newTrail[i + 1];
          const targetX = (prevPoint.x + nextPoint.x) / 2;
          const targetY = (prevPoint.y + nextPoint.y) / 2;
          
          newTrail[i] = {
            x: newTrail[i].x + (targetX - newTrail[i].x) * config.middleLerp,
            y: newTrail[i].y + (targetY - newTrail[i].y) * config.middleLerp,
          };
        }
        return newTrail;
      });
      frameId = requestAnimationFrame(updateTrail);
    };

    frameId = requestAnimationFrame(updateTrail);
    return () => cancelAnimationFrame(frameId);
  }, [selectedSpool, mousePos, spoolPos, difficulty, config.lerpSpeed, config.middleLerp]);

  const handleMouseMove = (e: React.MouseEvent | React.TouchEvent) => {
    if (selectedSpool === null) return;
    initAudio();
    
    const rect = containerRef.current?.getBoundingClientRect();
    if (!rect) return;
    const x = ('touches' in e ? e.touches[0].clientX : e.clientX) - rect.left;
    const y = ('touches' in e ? e.touches[0].clientY : e.clientY) - rect.top;
    
    const dx = x - mousePos.x;
    const dy = y - mousePos.y;
    const velocity = Math.sqrt(dx * dx + dy * dy);
    
    if (gainNodeRef.current && audioCtxRef.current && filterNodeRef.current) {
      const targetGain = Math.min(0.2, velocity * 0.015);
      gainNodeRef.current.gain.setTargetAtTime(targetGain, audioCtxRef.current.currentTime, 0.05);
      filterNodeRef.current.frequency.setTargetAtTime(600 + velocity * 20, audioCtxRef.current.currentTime, 0.1);
    }

    setMousePos({ x, y });
  };

  const playPhaseChangeSound = () => {
    if (!audioCtxRef.current) initAudio();
    const ctx = audioCtxRef.current!;
    const now = ctx.currentTime;
    
    const notes = [440, 554.37, 659.25]; // A4, C#5, E5
    notes.forEach((freq, i) => {
      const osc = ctx.createOscillator();
      const g = ctx.createGain();
      osc.type = 'sine';
      osc.frequency.setValueAtTime(freq, now + i * 0.1);
      g.gain.setValueAtTime(0, now + i * 0.1);
      g.gain.linearRampToValueAtTime(0.2, now + i * 0.1 + 0.05);
      g.gain.exponentialRampToValueAtTime(0.01, now + i * 0.1 + 0.3);
      osc.connect(g);
      g.connect(ctx.destination);
      osc.start(now + i * 0.1);
      osc.stop(now + i * 0.1 + 0.4);
    });
  };

  const playVictorySound = () => {
    if (!audioCtxRef.current) initAudio();
    const ctx = audioCtxRef.current!;
    const now = ctx.currentTime;
    
    const notes = [523.25, 659.25, 783.99, 1046.50]; // C5, E5, G5, C6
    notes.forEach((freq, i) => {
      const osc = ctx.createOscillator();
      const g = ctx.createGain();
      osc.type = 'triangle';
      osc.frequency.setValueAtTime(freq, now + i * 0.1);
      g.gain.setValueAtTime(0, now + i * 0.1);
      g.gain.linearRampToValueAtTime(0.3, now + i * 0.1 + 0.05);
      g.gain.exponentialRampToValueAtTime(0.01, now + i * 0.1 + 0.8);
      osc.connect(g);
      g.connect(ctx.destination);
      osc.start(now + i * 0.1);
      osc.stop(now + i * 0.1 + 1.0);
    });
  };

  const getSpoolCenter = (colIdx: number, threadIdx: number) => {
    const el = spoolRefs.current[colIdx]?.[threadIdx];
    const containerRect = containerRef.current?.getBoundingClientRect();
    if (!el || !containerRect) return { x: 0, y: 0 };
    
    const rect = el.getBoundingClientRect();
    return {
      x: rect.left - containerRect.left + rect.width / 2,
      y: rect.top - containerRect.top + rect.height / 2
    };
  };

  const handleSpoolClick = (colIdx: number, threadIdx: number) => {
    if (!difficulty || columns.length === 0) return;

    // Case 1: Selecting a starting spool
    if (selectedSpool === null) {
      const thread = columns[colIdx][threadIdx];
      // Can only start from a spool that has a right connection available
      if (colIdx === config.numColumns - 1) return; // Can't start from last column
      if (thread.connectedRight) return;
      
      // New restriction: must be first column or have a connection from left
      if (colIdx > 0 && !thread.connectedLeft) return;

      const center = getSpoolCenter(colIdx, threadIdx);
      setSpoolPos(center);
      setMousePos(center);
      setTrail(Array(config.trailPoints).fill(center));
      setSelectedSpool({ col: colIdx, idx: threadIdx });
    } 
    // Case 2: Selecting a target spool
    else {
      // Must connect to a column to the right
      if (colIdx <= selectedSpool.col) {
        setSelectedSpool(null);
        return;
      }

      // In advanced/difficult levels, must connect to the immediate next column (no automatic jumps)
      if (difficulty !== 'facil' && colIdx !== selectedSpool.col + 1) {
        setSelectedSpool(null);
        return;
      }
      
      const startThread = columns[selectedSpool.col][selectedSpool.idx];
      const endThread = columns[colIdx][threadIdx];

      // Colors must match
      if (startThread.color === endThread.color) {
        const newConnections = [...connections];
        const newColumns = [...columns];
        
        // Connect step by step if user skipped columns
        let currentIdx = selectedSpool.idx;
        for (let c = selectedSpool.col; c < colIdx; c++) {
          // Find the matching color thread in the NEXT column
          const nextColIdx = c + 1;
          const targetThreadIdxInNextCol = newColumns[nextColIdx].findIndex(t => t.color === startThread.color);
          
          if (targetThreadIdxInNextCol !== -1) {
            // Only add connection if not already connected
            const exists = newConnections.some(conn => conn.phase === c && conn.from === currentIdx && conn.to === targetThreadIdxInNextCol);
            if (!exists) {
              newConnections.push({
                phase: c,
                from: currentIdx,
                to: targetThreadIdxInNextCol,
                color: startThread.color
              });
              newColumns[c][currentIdx].connectedRight = true;
              newColumns[nextColIdx][targetThreadIdxInNextCol].connectedLeft = true;
            }
            currentIdx = targetThreadIdxInNextCol;
          }
        }

        setConnections(newConnections);
        setColumns(newColumns);
        setSelectedSpool(null);
        
        if (gainNodeRef.current && audioCtxRef.current) {
          gainNodeRef.current.gain.setTargetAtTime(0, audioCtxRef.current.currentTime, 0.1);
        }

        // Check win condition: all colors connected from col 0 to last col
        const isComplete = colors.every(color => {
          let currentCol = 0;
          let currentIdx = newColumns[0].findIndex(t => t.color === color);
          while (currentCol < config.numColumns - 1) {
            const conn = newConnections.find(c => c.phase === currentCol && c.from === currentIdx && c.color === color);
            if (!conn) return false;
            currentIdx = conn.to;
            currentCol++;
          }
          return true;
        });

        if (isComplete) {
          if (completedLevels < 2) {
            // Move to next random level
            playPhaseChangeSound();
            setTimeout(() => {
              setCompletedLevels(prev => prev + 1);
              setDifficulty(getRandomDifficulty());
              // Resetting difficulty triggers the useEffect that regenerates columns/connections
            }, 1000);
          } else {
            // Finished all 3 levels
            playVictorySound();
            setTimeout(onComplete, 1000);
          }
        }
      } else {
        setSelectedSpool(null);
        if (gainNodeRef.current && audioCtxRef.current) {
          gainNodeRef.current.gain.setTargetAtTime(0, audioCtxRef.current.currentTime, 0.1);
        }
      }
    }
  };

  const getSnakePath = (points: {x: number, y: number}[]) => {
    if (points.length < 2) return "";
    let path = `M ${points[0].x} ${points[0].y}`;
    for (let i = 1; i < points.length - 1; i++) {
      const xc = (points[i].x + points[i + 1].x) / 2;
      const yc = (points[i].y + points[i + 1].y) / 2;
      path += ` Q ${points[i].x} ${points[i].y}, ${xc} ${yc}`;
    }
    path += ` L ${points[points.length - 1].x} ${points[points.length - 1].y}`;
    return path;
  };

  return (
    <div 
      ref={containerRef}
      className="relative h-64 flex justify-between items-center px-8 bg-[#1a1410] rounded-lg border-2 border-amber-900/30 touch-none overflow-hidden"
      onMouseMove={handleMouseMove}
      onTouchMove={handleMouseMove}
      onMouseUp={() => {
        if (gainNodeRef.current && audioCtxRef.current) {
          gainNodeRef.current.gain.setTargetAtTime(0, audioCtxRef.current.currentTime, 0.1);
        }
      }}
    >
      <div className="absolute top-2 left-2 z-20 flex gap-2 items-center">
        <div className="px-2 py-1 bg-amber-900/50 rounded border border-amber-500/30 text-[10px] text-amber-200 font-medieval uppercase tracking-widest">
          Tapestry {completedLevels + 1}/3
        </div>
        <div className="px-2 py-1 bg-amber-900/50 rounded border border-amber-500/30 text-[10px] text-amber-200 font-medieval uppercase tracking-widest">
          {difficulty === 'facil' ? 'Easy' : difficulty === 'avanzado' ? 'Advanced' : 'Difficult'}
        </div>
      </div>

      <div className="absolute inset-0 opacity-20 pointer-events-none" style={{ backgroundImage: 'url("https://www.transparenttextures.com/patterns/natural-paper.png")' }} />

      {columns.map((col, colIdx) => (
        <div key={colIdx} className="flex flex-col gap-4 z-10">
          {col.map((thread, threadIdx) => {
            const isSelected = selectedSpool?.col === colIdx && selectedSpool?.idx === threadIdx;
            const isSelectable = (selectedSpool === null && colIdx < config.numColumns - 1 && !thread.connectedRight && (colIdx === 0 || thread.connectedLeft)) ||
                               (selectedSpool !== null && 
                                (difficulty === 'facil' ? colIdx > selectedSpool.col : colIdx === selectedSpool.col + 1) && 
                                thread.color === columns[selectedSpool.col][selectedSpool.idx].color && 
                                !thread.connectedLeft);
            
            return (
              <button
                key={threadIdx}
                ref={el => {
                  if (spoolRefs.current[colIdx]) spoolRefs.current[colIdx][threadIdx] = el;
                }}
                onClick={() => handleSpoolClick(colIdx, threadIdx)}
                className={`w-8 h-8 rounded-full transition-all relative flex items-center justify-center 
                  ${isSelected ? 'scale-110' : ''} 
                  ${isSelectable ? 'ring-2 ring-amber-500/50 ring-offset-2 ring-offset-[#1a1410] cursor-pointer' : 'cursor-default'}
                  ${(thread.connectedLeft && thread.connectedRight) ? 'opacity-40' : ''}
                `}
              >
                <div className="absolute inset-0 bg-[#5d4037] rounded-full border-2 border-[#3e2723] shadow-lg" />
                <div className="w-4 h-4 rounded-full z-10 shadow-inner" style={{ backgroundColor: thread.color }} />
                {isSelected && (
                  <motion.div layoutId="glow" className="absolute inset-[-4px] border-2 border-amber-400 rounded-full animate-pulse" />
                )}
              </button>
            );
          })}
        </div>
      ))}

      <svg className="absolute inset-0 pointer-events-none w-full h-full">
        <defs>
          <filter id="ropeShadow" x="-50%" y="-50%" width="200%" height="200%">
            <feGaussianBlur in="SourceAlpha" stdDeviation="2" />
            <feOffset dx="1" dy="2" result="offsetblur" />
            <feComponentTransfer>
              <feFuncA type="linear" slope="0.5" />
            </feComponentTransfer>
            <feMerge>
              <feMergeNode />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
          
          {/* Subtle organic edge without glitches */}
          <filter id="ropeOrganic">
            <feTurbulence type="fractalNoise" baseFrequency="0.5" numOctaves="3" result="noise" />
            <feDisplacementMap in="SourceGraphic" in2="noise" scale="1.5" />
          </filter>

          {/* Fiber noise overlay */}
          <filter id="ropeNoise">
            <feTurbulence type="fractalNoise" baseFrequency="0.8" numOctaves="2" result="noise" />
            <feComposite operator="in" in="noise" in2="SourceGraphic" />
            <feBlend mode="multiply" in="SourceGraphic" />
          </filter>
        </defs>

        {connections.map((conn, i) => {
          const start = getSpoolCenter(conn.phase, conn.from);
          const end = getSpoolCenter(conn.phase + 1, conn.to);
          
          if (start.x === 0 && start.y === 0) return null;

          const midX = (start.x + end.x) / 2;
          const midY = (start.y + end.y) / 2 + Math.min(30, Math.abs(end.x - start.x) * 0.15);
          const path = `M ${start.x} ${start.y} Q ${midX} ${midY}, ${end.x} ${end.y}`;
          
          return (
            <g key={i} filter="url(#ropeShadow)">
              {/* 1. Base Body */}
              <motion.path
                initial={{ pathLength: 0, opacity: 0 }}
                animate={{ pathLength: 1, opacity: 1 }}
                d={path}
                stroke={conn.color}
                strokeWidth="12"
                fill="none"
                strokeLinecap="round"
                filter="url(#ropeOrganic)"
              />
              
              {/* 2. Braided Twists (Dark Grooves) */}
              <motion.path
                initial={{ pathLength: 0, opacity: 0 }}
                animate={{ pathLength: 1, opacity: 0.5 }}
                d={path}
                stroke="black"
                strokeWidth="12"
                fill="none"
                strokeLinecap="round"
                strokeDasharray="6,12"
                style={{ strokeDashoffset: 3 }}
              />

              {/* 3. Braided Twists (Light Peaks) */}
              <motion.path
                initial={{ pathLength: 0, opacity: 0 }}
                animate={{ pathLength: 1, opacity: 0.3 }}
                d={path}
                stroke="white"
                strokeWidth="10"
                fill="none"
                strokeLinecap="round"
                strokeDasharray="6,12"
                style={{ strokeDashoffset: 9 }}
              />

              {/* 4. Fiber Texture Overlay */}
              <motion.path
                initial={{ pathLength: 0, opacity: 0 }}
                animate={{ pathLength: 1, opacity: 0.15 }}
                d={path}
                stroke="black"
                strokeWidth="12"
                fill="none"
                strokeLinecap="round"
                filter="url(#ropeNoise)"
              />
            </g>
          );
        })}

        {selectedSpool !== null && trail.length > 0 && (
          <g filter="url(#ropeShadow)">
            {/* Base */}
            <path
              d={getSnakePath(trail)}
              stroke={columns[selectedSpool.col][selectedSpool.idx].color}
              strokeWidth="12"
              fill="none"
              strokeLinecap="round"
              strokeLinejoin="round"
              filter="url(#ropeOrganic)"
            />
            {/* Grooves */}
            <path
              d={getSnakePath(trail)}
              stroke="black"
              strokeWidth="12"
              fill="none"
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeDasharray="6,12"
              style={{ strokeDashoffset: 3 }}
              opacity="0.5"
            />
            {/* Peaks */}
            <path
              d={getSnakePath(trail)}
              stroke="white"
              strokeWidth="10"
              fill="none"
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeDasharray="6,12"
              style={{ strokeDashoffset: 9 }}
              opacity="0.3"
            />
            {/* Noise */}
            <path
              d={getSnakePath(trail)}
              stroke="black"
              strokeWidth="12"
              fill="none"
              strokeLinecap="round"
              strokeLinejoin="round"
              filter="url(#ropeNoise)"
              opacity="0.15"
            />
          </g>
        )}
      </svg>
    </div>
  );
};



// --- Main App ---

export default function App() {
  const [activeTask, setActiveTask] = useState<TaskType>(null);
  const [completedTasks, setCompletedTasks] = useState<TaskType[]>([]);

  const handleComplete = () => {
    if (activeTask && !completedTasks.includes(activeTask)) {
      setCompletedTasks([...completedTasks, activeTask]);
    }
    setActiveTask(null);
  };

  const tasks = [
    { id: 'tapestry', name: 'Mend Tapestry', icon: Scissors, color: 'text-red-700' },
  ] as const;

  return (
    <div className="min-h-screen bg-[#1a1410] text-amber-100 flex flex-col items-center justify-center p-4 font-medieval selection:bg-amber-500/30 overflow-hidden">
      {/* Background Effects */}
      <div className="fixed inset-0 pointer-events-none">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,_var(--tw-gradient-stops))] from-[#2d241e] via-transparent to-transparent opacity-50" />
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'url("https://www.transparenttextures.com/patterns/parchment.png")' }} />
        
        {/* Floating Embers */}
        {[...Array(10)].map((_, i) => (
          <motion.div
            key={i}
            className="absolute w-1 h-1 bg-orange-500 rounded-full blur-[1px]"
            initial={{ 
              x: Math.random() * 100 + '%', 
              y: '110%', 
              opacity: 0 
            }}
            animate={{ 
              y: '-10%', 
              opacity: [0, 0.8, 0],
              x: (Math.random() * 100 - 50) + 'px'
            }}
            transition={{ 
              duration: 5 + Math.random() * 5, 
              repeat: Infinity, 
              delay: Math.random() * 5 
            }}
          />
        ))}
      </div>

      <AnimatePresence mode="wait">
        {!activeTask ? (
          <motion.div 
            key="dashboard"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className="w-full max-w-2xl z-10"
          >
            <div className="text-center mb-12">
              <div className="flex justify-center gap-4 mb-4">
                <Skull className="text-amber-900/50" size={32} />
                <Shield className="text-amber-600" size={48} />
                <Skull className="text-amber-900/50" size={32} />
              </div>
              <h1 className="text-6xl font-medieval font-black tracking-tighter uppercase mb-2 bg-gradient-to-b from-amber-100 to-amber-900 bg-clip-text text-transparent drop-shadow-lg">
                Village Tasks
              </h1>
              <p className="text-amber-800 font-bold uppercase tracking-[0.3em] text-sm">Shadow of the Wolf</p>
            </div>

            <div className="flex justify-center">
              {tasks.map((task) => {
                const isCompleted = completedTasks.includes(task.id);
                return (
                  <button
                    key={task.id}
                    onClick={() => setActiveTask(task.id)}
                    className={`relative group p-6 rounded-lg border-4 transition-all flex items-center gap-4 text-left overflow-hidden w-full max-w-md ${
                      isCompleted 
                      ? 'bg-green-900/10 border-green-900/30 grayscale opacity-50' 
                      : 'bg-[#2d241e] border-[#4a3b2f] hover:border-amber-700 hover:bg-[#3d342e] active:scale-95 shadow-xl'
                    }`}
                    style={{ backgroundImage: 'url("https://www.transparenttextures.com/patterns/dark-leather.png")' }}
                  >
                    <div className={`p-4 rounded-lg bg-[#1a1410] border-2 border-amber-900/50 ${task.color} shadow-inner`}>
                      <task.icon size={32} />
                    </div>
                    <div>
                      <div className="font-medieval font-bold uppercase tracking-tight text-xl text-amber-100">{task.name}</div>
                      <div className="text-xs text-amber-800 font-bold uppercase tracking-widest">
                        {isCompleted ? 'Finished' : 'Awaiting Labor'}
                      </div>
                    </div>
                    {isCompleted && (
                      <div className="absolute right-4 top-1/2 -translate-y-1/2 text-green-700">
                        <CheckCircle2 size={32} />
                      </div>
                    )}
                    <div className="absolute -right-4 -bottom-4 opacity-5 group-hover:opacity-10 transition-opacity">
                      <task.icon size={120} />
                    </div>
                  </button>
                );
              })}
            </div>

            {completedTasks.length === tasks.length && (
              <motion.div 
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                className="mt-12 p-6 bg-amber-900/20 border-2 border-amber-900/50 rounded-lg text-center shadow-2xl"
              >
                <div className="text-amber-400 font-medieval font-black text-2xl uppercase tracking-widest drop-shadow-md">The Village is Secure</div>
                <button 
                  onClick={() => setCompletedTasks([])}
                  className="mt-4 text-xs font-bold uppercase tracking-widest text-amber-700 hover:text-amber-500 transition-colors"
                >
                  Reset Chores
                </button>
              </motion.div>
            )}
          </motion.div>
        ) : (
          <div key="task-container" className="z-10 w-full flex justify-center">
            {activeTask === 'tapestry' && (
              <TaskPanel title="Mend Tapestry" onBack={() => setActiveTask(null)}>
                <TapestryTask onComplete={handleComplete} />
              </TaskPanel>
            )}
          </div>
        )}
      </AnimatePresence>

      <div className="fixed bottom-4 text-[10px] font-medieval text-amber-900 uppercase tracking-[0.5em] opacity-50">
        Era: Dark Ages | Village: Ravenwood | AIS-MEDIEVAL
      </div>
    </div>
  );
}
