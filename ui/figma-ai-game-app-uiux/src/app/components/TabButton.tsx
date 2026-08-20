import { motion } from "motion/react";
import { LucideIcon } from "lucide-react";

interface TabButtonProps {
  icon: LucideIcon;
  label: string;
  active: boolean;
  onClick: () => void;
  badge?: number;
}

export function TabButton({ icon: Icon, label, active, onClick, badge }: TabButtonProps) {
  return (
    <motion.button
      onClick={onClick}
      className="relative flex-1 flex flex-col items-center justify-center gap-1 py-3 rounded-xl"
      style={{
        backgroundColor: active ? "#ffffff20" : "transparent",
      }}
      whileTap={{ scale: 0.95 }}
    >
      <div className="relative">
        <Icon
          className="w-6 h-6"
          style={{ color: active ? "#60a5fa" : "#ffffff60" }}
        />
        {badge !== undefined && badge > 0 && (
          <motion.div
            className="absolute -top-2 -right-2 w-5 h-5 bg-red-500 rounded-full flex items-center justify-center"
            initial={{ scale: 0 }}
            animate={{ scale: 1 }}
            transition={{ type: "spring", stiffness: 500 }}
          >
            <span className="text-white text-xs font-bold">{badge}</span>
          </motion.div>
        )}
      </div>
      <span
        className="text-xs font-medium"
        style={{ color: active ? "#60a5fa" : "#ffffff60" }}
      >
        {label}
      </span>
      {active && (
        <motion.div
          className="absolute bottom-0 left-1/2 -translate-x-1/2 w-12 h-1 bg-blue-400 rounded-full"
          layoutId="activeTab"
        />
      )}
    </motion.button>
  );
}
