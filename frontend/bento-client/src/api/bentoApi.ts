import axios from "axios";
import type {
  CreateMenuItemRequest,
  CreateOrderRequest,
  CreateUserRequest,
  MenuCacheResponse,
  MenuItem,
  Order,
  User,
} from "../types";

const gatewayBaseUrl =
  import.meta.env.VITE_BENTO_GATEWAY_BASE_URL ||
  "http://localhost:5000/gateway";

const api = axios.create({
  baseURL: gatewayBaseUrl,
  timeout: 10000,
});

export const bentoApi = {
  async getMenu(): Promise<MenuItem[]> {
    const { data } = await api.get<MenuItem[]>("/api/menu");
    return data;
  },

  async createMenu(payload: CreateMenuItemRequest): Promise<MenuItem> {
    const { data } = await api.post<MenuItem>("/api/menu", payload);
    return data;
  },

  async getOrders(): Promise<Order[]> {
    const { data } = await api.get<Order[]>("/api/order");
    return data;
  },

  async createOrder(payload: CreateOrderRequest): Promise<Order> {
    const { data } = await api.post<Order>("/api/order", payload);
    return data;
  },

  async updateOrderStatus(id: number, status: string): Promise<void> {
    await api.patch(`/api/order/${id}/status`, { status });
  },

  async getUsers(): Promise<User[]> {
    const { data } = await api.get<User[]>("/api/user");
    return data;
  },

  async createUser(payload: CreateUserRequest): Promise<User> {
    const { data } = await api.post<User>("/api/user", payload);
    return data;
  },

  async getMenuCache(): Promise<MenuCacheResponse> {
    const { data } = await api.get<MenuCacheResponse>("/api/cache/menu");
    return data;
  },

  async clearMenuCache(): Promise<void> {
    await api.delete("/api/cache/menu");
  },
};
