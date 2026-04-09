import { FormEvent, useEffect, useMemo, useState } from "react";
import { bentoApi } from "../api/bentoApi";
import type { MenuItem, Order, User } from "../types";

export function OrderPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [menu, setMenu] = useState<MenuItem[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [selectedUserId, setSelectedUserId] = useState<number>(0);
  const [quantities, setQuantities] = useState<Record<number, number>>({});

  const availableMenu = useMemo(
    () => menu.filter((m) => m.isAvailable),
    [menu],
  );

  const loadAll = async () => {
    const [userResult, menuResult, orderResult] = await Promise.all([
      bentoApi.getUsers(),
      bentoApi.getMenu(),
      bentoApi.getOrders(),
    ]);

    setUsers(userResult);
    setMenu(menuResult);
    setOrders(orderResult);

    if (userResult.length > 0 && selectedUserId === 0) {
      setSelectedUserId(userResult[0].id);
    }
  };

  useEffect(() => {
    void loadAll();
  }, []);

  const createOrder = async (event: FormEvent) => {
    event.preventDefault();

    const items = Object.entries(quantities)
      .map(([menuItemId, quantity]) => ({
        menuItemId: Number(menuItemId),
        quantity,
      }))
      .filter((item) => item.quantity > 0);

    if (selectedUserId <= 0 || items.length === 0) {
      alert("請選擇使用者並至少填一個品項數量。");
      return;
    }

    await bentoApi.createOrder({
      userId: selectedUserId,
      items,
    });

    setQuantities({});
    await loadAll();
  };

  const updateStatus = async (orderId: number, status: string) => {
    await bentoApi.updateOrderStatus(orderId, status);
    await loadAll();
  };

  return (
    <section className="panel">
      <h2>訂單管理</h2>
      <form className="order-form" onSubmit={createOrder}>
        <label>
          訂購人
          <select
            value={selectedUserId}
            onChange={(event) => setSelectedUserId(Number(event.target.value))}
          >
            {users.map((user) => (
              <option key={user.id} value={user.id}>
                {user.name}（{user.email}）
              </option>
            ))}
          </select>
        </label>

        <div className="menu-grid">
          {availableMenu.map((item) => (
            <label key={item.id} className="menu-card">
              <span>{item.name}</span>
              <small>NT$ {item.price}</small>
              <input
                type="number"
                min={0}
                max={20}
                value={quantities[item.id] ?? 0}
                onChange={(event) =>
                  setQuantities((current) => ({
                    ...current,
                    [item.id]: Number(event.target.value),
                  }))
                }
              />
            </label>
          ))}
        </div>

        <button type="submit">送出訂單</button>
      </form>

      <h3>最近訂單</h3>
      <table>
        <thead>
          <tr>
            <th>訂單編號</th>
            <th>使用者</th>
            <th>總金額</th>
            <th>狀態</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          {orders.map((order) => (
            <tr key={order.id}>
              <td>{order.id}</td>
              <td>{order.userName ?? `#${order.userId}`}</td>
              <td>NT$ {order.totalAmount}</td>
              <td>{order.status}</td>
              <td>
                <select
                  value={order.status}
                  onChange={(event) =>
                    void updateStatus(order.id, event.target.value)
                  }
                >
                  <option value="待確認">待確認</option>
                  <option value="製作中">製作中</option>
                  <option value="已完成">已完成</option>
                  <option value="已取消">已取消</option>
                </select>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
