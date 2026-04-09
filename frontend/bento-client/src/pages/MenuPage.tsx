import { FormEvent, useEffect, useState } from "react";
import { bentoApi } from "../api/bentoApi";
import type { MenuItem } from "../types";

export function MenuPage() {
  const [menu, setMenu] = useState<MenuItem[]>([]);
  const [name, setName] = useState("滷雞腿便當");
  const [price, setPrice] = useState(125);
  const [isAvailable, setIsAvailable] = useState(true);
  const [loading, setLoading] = useState(false);

  const loadMenu = async () => {
    setLoading(true);
    try {
      const result = await bentoApi.getMenu();
      setMenu(result);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadMenu();
  }, []);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    await bentoApi.createMenu({ name, price, isAvailable });
    setName("");
    setPrice(100);
    setIsAvailable(true);
    await loadMenu();
  };

  return (
    <section className="panel">
      <h2>菜單管理</h2>
      <p className="hint">新增品項後會自動清除 Redis 菜單快取。</p>

      <form className="inline-form" onSubmit={onSubmit}>
        <input
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="菜名，例如：排骨便當"
          required
        />
        <input
          type="number"
          min={1}
          max={9999}
          value={price}
          onChange={(event) => setPrice(Number(event.target.value))}
          required
        />
        <label>
          <input
            type="checkbox"
            checked={isAvailable}
            onChange={(event) => setIsAvailable(event.target.checked)}
          />
          上架
        </label>
        <button type="submit">新增菜單</button>
      </form>

      {loading ? (
        <p>載入中...</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>編號</th>
              <th>名稱</th>
              <th>價格</th>
              <th>狀態</th>
            </tr>
          </thead>
          <tbody>
            {menu.map((item) => (
              <tr key={item.id}>
                <td>{item.id}</td>
                <td>{item.name}</td>
                <td>NT$ {item.price}</td>
                <td>{item.isAvailable ? "供應中" : "停售"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
