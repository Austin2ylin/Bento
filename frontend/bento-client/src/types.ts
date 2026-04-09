export interface User {
  id: number;
  name: string;
  email: string;
  createdAt?: string;
}

export interface MenuItem {
  id: number;
  name: string;
  price: number;
  isAvailable: boolean;
  updatedAt?: string;
}

export interface OrderItem {
  menuItemId: number;
  menuName?: string;
  quantity: number;
  unitPrice: number;
}

export interface Order {
  id: number;
  userId: number;
  userName?: string;
  status: string;
  totalAmount: number;
  orderedAt: string;
  items: OrderItem[];
}

export interface CreateUserRequest {
  name: string;
  email: string;
}

export interface CreateMenuItemRequest {
  name: string;
  price: number;
  isAvailable: boolean;
}

export interface CreateOrderRequest {
  userId: number;
  items: {
    menuItemId: number;
    quantity: number;
  }[];
}

export interface MenuCacheResponse {
  source: "redis" | "database";
  data: MenuItem[];
}
