import { mount, flushPromises } from '@vue/test-utils'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import OrderView from '@/views/Order/OrderView.vue'

vi.mock('@/stores/auth', () => ({
  useAuthStore: () => ({ logout: vi.fn() })
}))

vi.mock('@/api/client', () => ({
  orderApi: {
    getAll: vi.fn().mockResolvedValue({ data: [] }),
    getById: vi.fn(),
    create: vi.fn().mockResolvedValue({ data: { order_id: 99 } }),
    update: vi.fn().mockResolvedValue({ data: { order_id: 99 } })
  },
  customerApi: {
    getAll: vi.fn().mockResolvedValue({
      data: [
        { customer_id: 1, full_name: 'Alice' },
        { customer_id: 2, full_name: 'Bob' }
      ]
    })
  },
  productApi: {
    getAll: vi.fn().mockResolvedValue({
      data: [
        { product_id: 'p1', product_name: 'Product 1' },
        { product_id: 'p2', product_name: 'Product 2' },
        { product_id: 'p3', product_name: 'Product 3' }
      ]
    })
  },
  stockApi: {
    getAll: vi.fn().mockResolvedValue({
      data: [
        { stock_id: 's1', stock_name: 'Kho Hà Nội' },
        { stock_id: 's2', stock_name: 'Kho Sài Gòn' }
      ]
    })
  },
  productPricesApi: {
    getStock: vi.fn((pid, sid) => Promise.resolve({ data: { quantity: 10 } })),
    getSellingPrice: vi.fn().mockResolvedValue({ data: { price: 100 } })
  }
}))

const factory = () => mount(OrderView, {
  attachTo: document.body,
  global: {
    stubs: {
      'v-data-table': true,
      'v-dialog': {
        template: '<div data-testid="v-dialog-stub"><slot /></div>'
      }
    }
  }
})

describe('OrderView - hành vi 1: edit từng dòng trong detail dialog', () => {
  beforeEach(async () => {
    const { orderApi } = await import('@/api/client')
    orderApi.getById.mockResolvedValue({
      data: {
        order_id: 7,
        customer_id: 1,
        order_code: 'ORD-7',
        total_amount: 50,
        status: 'pending',
        order_date: '2026-06-05',
        items: [
          { product_id: 'p1', quantity: 1, unit_price: 10 },
          { product_id: 'p2', quantity: 2, unit_price: 20 }
        ]
      }
    })
  })

  it('user có thể sửa quantity của item thứ nhất (không phải chỉ item cuối)', async () => {
    const wrapper = factory()
    await flushPromises()

    await wrapper.vm.openDetailDialog({ order_id: 7 })
    await flushPromises()

    await wrapper.find('[data-testid="btn-start-edit"]').trigger('click')
    await flushPromises()

    const firstQtyInput = wrapper.find('[data-testid="detail-item-qty-p1"] input')
    expect(firstQtyInput.exists()).toBe(true)

    await firstQtyInput.setValue('5')
    await flushPromises()

    const items = wrapper.vm.detailData.items
    expect(items[0].quantity).toBe(5)
    expect(items[1].quantity).toBe(2)

    expect(wrapper.vm.detailTotal).toBe(5 * 10 + 2 * 20)
  })
})

describe('OrderView - hành vi 2: chọn kho reload tồn kho', () => {
  beforeEach(async () => {
    const { productPricesApi } = await import('@/api/client')
    productPricesApi.getStock.mockImplementation((pid, sid) =>
      Promise.resolve({ data: { quantity: sid === 's1' ? 10 : 50 } })
    )
  })

  it('đổi stock_id trong create dialog refetch productStocks với stockId mới', async () => {
    const { productPricesApi } = await import('@/api/client')
    const wrapper = factory()
    await flushPromises()

    await wrapper.find('[data-testid="btn-open-create"]').trigger('click')
    await flushPromises()

    const stockSelect = wrapper.find('[data-testid="stock-select-create"]')
    expect(stockSelect.exists()).toBe(true)

    productPricesApi.getStock.mockClear()
    wrapper.vm.formData.stock_id = 's2'
    await wrapper.vm.onCreateStockChange('s2')
    await flushPromises()

    expect(productPricesApi.getStock).toHaveBeenCalledWith('p1', 's2')
    expect(productPricesApi.getStock).toHaveBeenCalledWith('p2', 's2')
    expect(productPricesApi.getStock).toHaveBeenCalledWith('p3', 's2')
    expect(wrapper.vm.productStocks.p1).toBe(50)
  })

  it('đổi stock_id trong edit dialog cũng refetch productStocks', async () => {
    const { productPricesApi, orderApi } = await import('@/api/client')
    orderApi.getById.mockResolvedValue({
      data: {
        order_id: 8,
        customer_id: 1,
        order_code: 'ORD-8',
        total_amount: 30,
        status: 'pending',
        order_date: '2026-06-05',
        items: [{ product_id: 'p1', quantity: 1, unit_price: 30 }]
      }
    })
    const wrapper = factory()
    await flushPromises()

    await wrapper.vm.openDetailDialog({ order_id: 8 })
    await flushPromises()

    const stockSelect = wrapper.find('[data-testid="stock-select-edit"]')
    expect(stockSelect.exists()).toBe(true)

    productPricesApi.getStock.mockClear()
    wrapper.vm.detailData.stock_id = 's2'
    await wrapper.vm.onEditStockChange('s2')
    await flushPromises()

    expect(productPricesApi.getStock).toHaveBeenCalledWith('p1', 's2')
    expect(wrapper.vm.productStocks.p1).toBe(50)
  })
})

describe('OrderView - getMaxQuantity hiển thị tồn kho thật', () => {
  it('trả về 0 khi tồn kho = 0 (kho trống), không fallback về 999', async () => {
    const wrapper = factory()
    await flushPromises()
    wrapper.vm.productStocks = { p1: 0 }
    expect(wrapper.vm.getMaxQuantity('p1')).toBe(0)
  })

  it('trả về giá trị thật khi tồn kho > 0', async () => {
    const wrapper = factory()
    await flushPromises()
    wrapper.vm.productStocks = { p1: 5 }
    expect(wrapper.vm.getMaxQuantity('p1')).toBe(5)
  })

  it('fallback về 999 khi chưa fetch tồn kho (undefined)', async () => {
    const wrapper = factory()
    await flushPromises()
    wrapper.vm.productStocks = {}
    expect(wrapper.vm.getMaxQuantity('p1')).toBe(999)
  })
})

describe('OrderView - hành vi 3: arrow keys cho quantity input', () => {
  it('ArrowUp tăng itemQuantity, clamp theo max=stock', async () => {
    const wrapper = factory()
    await flushPromises()

    await wrapper.find('[data-testid="btn-open-create"]').trigger('click')
    await flushPromises()

    wrapper.vm.productStocks = { p1: 3 }
    wrapper.vm.selectedProduct = { product_id: 'p1' }
    wrapper.vm.itemQuantity = 2
    await flushPromises()

    const input = wrapper.find('[data-testid="item-qty-input"] input')
    expect(input.exists()).toBe(true)

    await input.trigger('keydown', { key: 'ArrowUp' })
    expect(wrapper.vm.itemQuantity).toBe(3)

    await input.trigger('keydown', { key: 'ArrowUp' })
    expect(wrapper.vm.itemQuantity).toBe(3)
  })

  it('ArrowDown giảm itemQuantity, clamp theo min=1', async () => {
    const wrapper = factory()
    await flushPromises()

    await wrapper.find('[data-testid="btn-open-create"]').trigger('click')
    await flushPromises()

    wrapper.vm.productStocks = { p1: 5 }
    wrapper.vm.selectedProduct = { product_id: 'p1' }
    wrapper.vm.itemQuantity = 2
    await flushPromises()

    const input = wrapper.find('[data-testid="item-qty-input"] input')

    await input.trigger('keydown', { key: 'ArrowDown' })
    expect(wrapper.vm.itemQuantity).toBe(1)

    await input.trigger('keydown', { key: 'ArrowDown' })
    expect(wrapper.vm.itemQuantity).toBe(1)
  })
})
