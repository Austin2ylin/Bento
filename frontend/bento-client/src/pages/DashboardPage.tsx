import { FormEvent, useEffect, useState } from "react";
import { bentoApi } from "../api/bentoApi";
import type { MenuCacheResponse, Order, User } from "../types";

export function DashboardPage() {
  const [orders, setOrders] = useState<Order[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [cacheInfo, setCacheInfo] = useState<MenuCacheResponse | null>(null);
  const [newUserName, setNewUserName] = useState("王小明");
  const [newUserEmail, setNewUserEmail] = useState("user@example.com");

  const refresh = async () => {
    const [ordersResult, usersResult] = await Promise.all([
      bentoApi.getOrders(),
      bentoApi.getUsers(),
    ]);
    setOrders(ordersResult);
    setUsers(usersResult);
  };

  useEffect(() => {
    void refresh();
  }, []);

  const loadCache = async () => {
    const data = await bentoApi.getMenuCache();
    setCacheInfo(data);
  };

  const clearCache = async () => {
    await bentoApi.clearMenuCache();
    setCacheInfo(null);
  };

  const createUser = async (event: FormEvent) => {
    event.preventDefault();
    await bentoApi.createUser({
      name: newUserName,
      email: newUserEmail,
    });

    setNewUserName("");
    setNewUserEmail("");
    await refresh();
  };

  const completed = orders.filter((order) => order.status === "已完成").length;
  const pending = orders.filter((order) => order.status === "待確認").length;

  return (
    <section className="panel">
      <h2>營運儀表板</h2>

      <div className="stats">
        <article>
          <h4>使用者總數</h4>
          <strong>{users.length}</strong>
        </article>
        <article>
          <h4>訂單總數</h4>
          <strong>{orders.length}</strong>
        </article>
        <article>
          <h4>待確認</h4>
          <strong>{pending}</strong>
        </article>
        <article>
          <h4>已完成</h4>
          <strong>{completed}</strong>
        </article>
      </div>

      <form className="inline-form" onSubmit={createUser}>
        <input
          value={newUserName}
          onChange={(event) => setNewUserName(event.target.value)}
          placeholder="姓名"
          required
        />
        <input
          value={newUserEmail}
          onChange={(event) => setNewUserEmail(event.target.value)}
          placeholder="Email"
          type="email"
          required
        />
        <button type="submit">新增使用者</button>
      </form>

      <div className="actions">
        <button type="button" onClick={() => void loadCache()}>
          讀取菜單快取
        </button>
        <button type="button" onClick={() => void clearCache()}>
          清除菜單快取
        </button>
        <button type="button" onClick={() => void refresh()}>
          重新整理統計
        </button>
      </div>

      {cacheInfo ? (
        <p>
          快取來源：{cacheInfo.source === "redis" ? "Redis" : "Database"}，共{" "}
          {cacheInfo.data.length} 筆菜單
        </p>
      ) : (
        <p>尚未載入快取資料。</p>
      )}
    </section>
  );
}
