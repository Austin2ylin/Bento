import { useState } from "react";
import { DashboardPage } from "./pages/DashboardPage";
import { MenuPage } from "./pages/MenuPage";
import { OrderPage } from "./pages/OrderPage";

type ViewKey = "dashboard" | "menu" | "order";

export default function App() {
  const [view, setView] = useState<ViewKey>("dashboard");

  return (
    <main className="app-shell">
      <header>
        <h1>Bento 便當訂餐系統</h1>
        <p>React 18 + TypeScript + Vite</p>
      </header>

      <nav>
        <button
          className={view === "dashboard" ? "active" : ""}
          onClick={() => setView("dashboard")}
          type="button"
        >
          儀表板
        </button>
        <button
          className={view === "menu" ? "active" : ""}
          onClick={() => setView("menu")}
          type="button"
        >
          菜單管理
        </button>
        <button
          className={view === "order" ? "active" : ""}
          onClick={() => setView("order")}
          type="button"
        >
          訂單管理
        </button>
      </nav>

      {view === "dashboard" && <DashboardPage />}
      {view === "menu" && <MenuPage />}
      {view === "order" && <OrderPage />}
    </main>
  );
}
