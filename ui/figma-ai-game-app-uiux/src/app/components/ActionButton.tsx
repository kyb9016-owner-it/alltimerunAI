import { motion } from "motion/react";
import { LucideIcon } from "lucide-react";

interface ActionButtonProps {
  icon: LucideIcon;
  label: string;
  onClick: () => void;
  color?: string;
  disabled?: boolean;
}

export function ActionButton({
  icon: Icon,
  label,
  onClick,
  color = "#06b6d4",
  disabled = false,
}: ActionButtonProps) {
  return (
    <motion.button
      onClick={onClick}
      disabled={disabled}
      className="relative flex flex-col items-center justify-center gap-2 p-4 rounded-lg w-full"
      style={{
        background: `linear-gradient(135deg, ${color}15, ${color}05)`,
        border: `1px solid ${color}40`,
        boxShadow: `0 0 20px ${color}20`,
      }}
      whileHover={!disabled ? { scale: 1.05, boxShadow: `0 0 30px ${color}40` } : {}}
      whileTap={!disabled ? { scale: 0.95 } : {}}
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: disabled ? 0.4 : 1, y: 0 }}
      transition={{ duration: 0.3 }}
    >
      {/* 배경 애니메이션 */}
      <motion.div
        className="absolute inset-0 rounded-lg"
        style={{
          background: `radial-gradient(circle at center, ${color}20, transparent)`,
        }}
        animate={{
          opacity: [0, 0.5, 0],
        }}
        transition={{
          duration: 2,
          repeat: Infinity,
          ease: "easeInOut",
        }}
      />

      {/* 아이콘 */}
      <motion.div
        animate={!disabled ? {
          rotate: [0, 5, -5, 0],
        } : {}}
        transition={{
          duration: 2,
          repeat: Infinity,
          ease: "easeInOut",
        }}
      >
        <Icon className="w-6 h-6" style={{ color }} strokeWidth={1.5} />
      </motion.div>

      {/* 레이블 */}
      <span
        className="text-xs font-mono tracking-wide uppercase relative z-10"
        style={{ color }}
      >
        {label}
      </span>

      {/* 코너 장식 */}
      <div
        className="absolute top-0 left-0 w-2 h-2 border-t border-l"
        style={{ borderColor: color }}
      />
      <div
        className="absolute top-0 right-0 w-2 h-2 border-t border-r"
        style={{ borderColor: color }}
      />
      <div
        className="absolute bottom-0 left-0 w-2 h-2 border-b border-l"
        style={{ borderColor: color }}
      />
      <div
        className="absolute bottom-0 right-0 w-2 h-2 border-b border-r"
        style={{ borderColor: color }}
      />
    </motion.button>
  );
}
