import { motion, AnimatePresence } from "motion/react";
import { useState, useEffect } from "react";

interface CoinDisplayProps {
  coins: number;
  coinsPerSecond: number;
}

export function CoinDisplay({ coins, coinsPerSecond }: CoinDisplayProps) {
  const [showIncome, setShowIncome] = useState(false);

  useEffect(() => {
    if (coinsPerSecond > 0) {
      setShowIncome(true);
      const timer = setTimeout(() => setShowIncome(false), 1000);
      return () => clearTimeout(timer);
    }
  }, [coins, coinsPerSecond]);

  return (
    <div className="relative">
      <motion.div
        className="bg-gradient-to-r from-yellow-500/20 to-orange-500/20 backdrop-blur-md rounded-3xl px-6 py-4 border-2 border-yellow-500/50"
        animate={{
          boxShadow: [
            "0 0 20px rgba(234, 179, 8, 0.3)",
            "0 0 30px rgba(234, 179, 8, 0.5)",
            "0 0 20px rgba(234, 179, 8, 0.3)",
          ],
        }}
        transition={{
          duration: 2,
          repeat: Infinity,
        }}
      >
        <div className="flex items-center justify-center gap-3">
          <motion.span
            className="text-4xl"
            animate={{
              rotate: [0, 10, -10, 0],
            }}
            transition={{
              duration: 2,
              repeat: Infinity,
            }}
          >
            💎
          </motion.span>
          <div className="text-center">
            <motion.div
              className="text-3xl font-bold text-yellow-400"
              key={coins}
              initial={{ scale: 1.2 }}
              animate={{ scale: 1 }}
              transition={{ type: "spring", stiffness: 300 }}
            >
              {coins.toLocaleString()}
            </motion.div>
            {coinsPerSecond > 0 && (
              <div className="text-xs text-yellow-300/70">
                +{coinsPerSecond.toLocaleString()}/초
              </div>
            )}
          </div>
        </div>
      </motion.div>

      {/* 자동 수입 표시 */}
      <AnimatePresence>
        {showIncome && coinsPerSecond > 0 && (
          <motion.div
            className="absolute -top-8 left-1/2 -translate-x-1/2 text-green-400 font-bold text-sm"
            initial={{ opacity: 0, y: 0 }}
            animate={{ opacity: 1, y: -10 }}
            exit={{ opacity: 0, y: -20 }}
          >
            +{coinsPerSecond}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
