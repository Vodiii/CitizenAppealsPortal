// app.js
import { auth } from './auth.js';
import { api } from './api.js';
import { Router } from './router.js';
import { formatDate, getStatusText, showToast, openModal, closeModal, renderPagination } from './utils.js';

// Глобальные переменные
let currentMap = null;
let mapLayers = {};

// Инициализация аутентификации
auth.init();

// --- Функция обновления навигации (вызывается роутером) ---
window.updateNavigation = function() {
  const navMenu = document.getElementById('navMenu');
  const navAuth = document.getElementById('navAuth');
  const isAuth = auth.isAuthenticated();
  const roles = auth.userRoles;

  let menuHtml = '';
  if (isAuth) {
    if (roles.includes('Citizen')) {
      menuHtml += '<a href="#/appeals">Мои обращения</a>';
      menuHtml += '<a href="#/appeals/new">Подать обращение</a>';
    }
    if (roles.includes('Deputy')) {
      menuHtml += '<a href="#/deputy">Кабинет депутата</a>';
    }
    if (roles.includes('Admin')) {
      menuHtml += '<a href="#/admin">Админ-панель</a>';
    }
    menuHtml += '<a href="#/map">Карта</a>';
  } else {
    menuHtml += '<a href="#/">Главная</a>';
    menuHtml += '<a href="#/map">Карта</a>';
  }
  navMenu.innerHTML = menuHtml;

  let authHtml = '';
  if (isAuth) {
    authHtml = `<span>${auth.getUserName()}</span>`;
    authHtml += '<button class="btn btn-outline" id="logoutBtn">Выйти</button>';
  } else {
    authHtml = '<button class="btn btn-outline" id="loginBtn">Войти</button>';
    authHtml += '<button class="btn btn-primary" id="registerBtn">Регистрация</button>';
  }
  navAuth.innerHTML = authHtml;

  if (isAuth) {
    document.getElementById('logoutBtn')?.addEventListener('click', () => auth.logout());
  } else {
    document.getElementById('loginBtn')?.addEventListener('click', () => window.location.hash = '#/login');
    document.getElementById('registerBtn')?.addEventListener('click', () => window.location.hash = '#/register');
  }

  // Бургер-меню
  const burger = document.getElementById('burgerBtn');
  if (burger) {
    burger.addEventListener('click', () => {
      navMenu.classList.toggle('active');
    });
  }
};

// --- Определение маршрутов ---
const routes = {
  '/': { handler: homePage },
  '/login': { handler: loginPage },
  '/register': { handler: registerPage },
  '/appeals': { handler: appealsListPage, auth: true },
  '/appeals/new': { handler: newAppealPage, auth: true, roles: ['Citizen'] },
  '/appeals/:id': { handler: appealDetailPage, auth: true },
  '/admin': { handler: adminPage, auth: true, roles: ['Admin'] },
  '/deputy': { handler: deputyPage, auth: true, roles: ['Deputy'] },
  '/map': { handler: mapPage },
  '/404': { handler: notFoundPage }
};

const router = new Router(routes);

// --- Обработчики страниц ---

function homePage() {
  document.getElementById('mainContent').innerHTML = `
    <section class="hero card">
      <h1>Сообщите о проблеме вашему депутату</h1>
      <p>Подайте обращение, прикрепите фото и отслеживайте статус решения.</p>
      ${auth.isAuthenticated() && auth.hasRole('Citizen') ? '<a href="#/appeals/new" class="btn btn-success btn-lg">Подать обращение</a>' : ''}
    </section>
  `;
}

function loginPage() {
  document.getElementById('mainContent').innerHTML = `
    <div class="card">
      <h2>Вход</h2>
      <form id="loginForm">
        <div class="form-group">
          <label>Email</label>
          <input type="email" name="email" class="form-control" required>
        </div>
        <div class="form-group">
          <label>Пароль</label>
          <input type="password" name="password" class="form-control" required>
        </div>
        <button type="submit" class="btn btn-primary">Войти</button>
      </form>
      <p class="mt-20">Нет аккаунта? <a href="#/register">Зарегистрироваться</a></p>
    </div>
  `;
  document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const email = e.target.email.value;
    const password = e.target.password.value;
    try {
      await auth.login(email, password);
      showToast('Вход выполнен', 'success');
      window.location.hash = '#/';
    } catch (err) {
      showToast(err.message, 'danger');
    }
  });
}

function registerPage() {
  document.getElementById('mainContent').innerHTML = `
    <div class="card">
      <h2>Регистрация</h2>
      <form id="registerForm">
        <div class="form-group">
          <label>ФИО</label>
          <input type="text" name="fullName" class="form-control" required>
        </div>
        <div class="form-group">
          <label>Email</label>
          <input type="email" name="email" class="form-control" required>
        </div>
        <div class="form-group">
          <label>Пароль</label>
          <input type="password" name="password" class="form-control" required>
        </div>
        <div class="form-group">
          <label>Роль</label>
          <select name="role" class="form-control">
            <option value="Citizen">Гражданин</option>
            <option value="Deputy">Депутат</option>
          </select>
        </div>
        <button type="submit" class="btn btn-primary">Зарегистрироваться</button>
      </form>
      <p class="mt-20">Уже есть аккаунт? <a href="#/login">Войти</a></p>
    </div>
  `;
  document.getElementById('registerForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const data = {
      fullName: e.target.fullName.value,
      email: e.target.email.value,
      password: e.target.password.value,
      role: e.target.role.value
    };
    try {
      await api.register(data);
      showToast('Регистрация успешна. Теперь войдите.', 'success');
      window.location.hash = '#/login';
    } catch (err) {
      showToast(err.message, 'danger');
    }
  });
}

async function appealsListPage() {
  const content = document.getElementById('mainContent');
  content.innerHTML = `<div class="card">Загрузка...</div>`;
  try {
    const data = await api.getAppeals();
    let html = `
      <div class="card">
        <h2>Мои обращения</h2>
        <div class="appeals-grid">
    `;
    if (data.items.length === 0) {
      html += '<p>У вас пока нет обращений.</p>';
    } else {
      data.items.forEach(app => {
        html += `
          <div class="appeal-card">
            <div class="appeal-header">
              <span class="category">${app.category?.name || 'Без категории'}</span>
              <span class="status status-${app.status}">${getStatusText(app.status)}</span>
            </div>
            <h3>${app.title}</h3>
            <p class="address"><i class="fas fa-map-pin"></i> ${app.address}</p>
            <p class="description">${app.description.substring(0, 100)}...</p>
            <div class="appeal-footer">
              <span class="date">${formatDate(app.createdAt)}</span>
              <a href="#/appeals/${app.id}" class="link">Подробнее →</a>
            </div>
          </div>
        `;
      });
    }
    html += `</div></div>`;
    content.innerHTML = html;
  } catch (err) {
    content.innerHTML = `<div class="card">Ошибка загрузки: ${err.message}</div>`;
  }
}

async function appealDetailPage(params) {
  const id = params.id;
  const content = document.getElementById('mainContent');
  content.innerHTML = `<div class="card">Загрузка...</div>`;
  try {
    const appeal = await api.getAppeal(id);
    const isDeputyOrAdmin = auth.hasRole('Deputy') || auth.hasRole('Admin');
    const isOwner = appeal.citizenId === auth.getCurrentUserId();
    let html = `
      <div class="card">
        <h2>${appeal.title}</h2>
        <div class="flex-between">
          <span class="category">${appeal.category?.name}</span>
          <span class="status status-${appeal.status}">${getStatusText(appeal.status)}</span>
        </div>
        <p class="address"><i class="fas fa-map-pin"></i> ${appeal.address}</p>
        <p>${appeal.description}</p>
        <p><strong>Округ:</strong> ${appeal.district?.name || 'Не определён'}</p>
        <p><strong>Создано:</strong> ${formatDate(appeal.createdAt)}</p>
        ${appeal.photos?.length ? `<div class="photos">${appeal.photos.map(p => `<img src="http://localhost:5000/${p.filePath}" style="max-width:200px; margin:5px;">`).join('')}</div>` : ''}
    `;

    // Форма для депутата/админа (если округ совпадает или админ)
    if (isDeputyOrAdmin) {
      // Для депутата проверяем, что это его округ (упрощённо – админ видит всегда)
      const canEdit = auth.hasRole('Admin') || (auth.hasRole('Deputy') && appeal.districtId === await getDeputyDistrictId());
      if (canEdit) {
        html += `
          <div class="form-group">
            <label>Изменить статус</label>
            <select id="statusSelect" class="form-control">
              <option value="0" ${appeal.status === 0 ? 'selected' : ''}>Новое</option>
              <option value="1" ${appeal.status === 1 ? 'selected' : ''}>На рассмотрении</option>
              <option value="2" ${appeal.status === 2 ? 'selected' : ''}>В работе</option>
              <option value="3" ${appeal.status === 3 ? 'selected' : ''}>Выполнено</option>
              <option value="4" ${appeal.status === 4 ? 'selected' : ''}>Отклонено</option>
            </select>
            <button id="updateStatusBtn" class="btn btn-primary mt-10">Обновить статус</button>
          </div>
          <div class="form-group">
            <label>Ответ гражданину</label>
            <textarea id="responseText" class="form-control" rows="3"></textarea>
            <button id="sendResponseBtn" class="btn btn-success mt-10">Отправить ответ</button>
          </div>
        `;
      }
    }

    html += `<h3>Ответы</h3>`;
    if (appeal.responses?.length) {
      appeal.responses.forEach(r => {
        html += `<div class="card"><strong>${r.author?.fullName || 'Система'}</strong> (${formatDate(r.createdAt)})<p>${r.content}</p></div>`;
      });
    } else {
      html += '<p>Ответов пока нет.</p>';
    }

    if (auth.hasRole('Admin')) {
      html += `<button id="deleteAppealBtn" class="btn btn-danger mt-20">Удалить обращение</button>`;
    }

    html += `</div>`;
    content.innerHTML = html;

    // Обработчики
    if (isDeputyOrAdmin) {
      document.getElementById('updateStatusBtn')?.addEventListener('click', async () => {
        const newStatus = parseInt(document.getElementById('statusSelect').value);
        try {
          await api.updateStatus(id, newStatus);
          showToast('Статус обновлён', 'success');
          window.location.reload();
        } catch (err) {
          showToast(err.message, 'danger');
        }
      });
      document.getElementById('sendResponseBtn')?.addEventListener('click', async () => {
        const text = document.getElementById('responseText').value;
        if (!text) return showToast('Введите текст ответа', 'warning');
        try {
          await api.addResponse(id, text);
          showToast('Ответ отправлен', 'success');
          window.location.reload();
        } catch (err) {
          showToast(err.message, 'danger');
        }
      });
    }
    if (auth.hasRole('Admin')) {
      document.getElementById('deleteAppealBtn')?.addEventListener('click', async () => {
        if (confirm('Удалить обращение?')) {
          try {
            await api.deleteAppeal(id);
            showToast('Обращение удалено', 'success');
            window.location.hash = '#/appeals';
          } catch (err) {
            showToast(err.message, 'danger');
          }
        }
      });
    }
  } catch (err) {
    content.innerHTML = `<div class="card">Ошибка: ${err.message}</div>`;
  }
}

// Вспомогательная функция получения ID округа депутата (можно улучшить)
async function getDeputyDistrictId() {
  if (!auth.hasRole('Deputy')) return null;
  try {
    const appeals = await api.getAppeals({ pageSize: 1 });
    if (appeals.items.length > 0) return appeals.items[0].districtId;
    // Альтернативно можно запросить профиль (если будет эндпоинт)
    return null;
  } catch { return null; }
}

function newAppealPage() {
  document.getElementById('mainContent').innerHTML = `
    <div class="card">
      <h2>Подать обращение</h2>
      <form id="appealForm">
        <div class="form-group">
          <label>Заголовок</label>
          <input type="text" name="title" class="form-control" required>
        </div>
        <div class="form-group">
          <label>Категория</label>
          <select name="categoryId" class="form-control" id="categoryId" required></select>
        </div>
        <div class="form-group">
          <label>Описание</label>
          <textarea name="description" class="form-control" rows="4" required></textarea>
        </div>
        <div class="form-group">
          <label>Адрес</label>
          <input type="text" name="address" class="form-control" required>
        </div>
        <div class="form-group">
          <label>Укажите точку на карте</label>
          <div id="miniMap" class="map-container" style="height:300px;"></div>
          <input type="hidden" name="locationGeoJson" id="locationGeoJson" required>
        </div>
        <div class="form-group">
          <label>Фотографии</label>
          <input type="file" name="photos" multiple accept="image/*" class="form-control">
        </div>
        <button type="submit" class="btn btn-success">Отправить</button>
      </form>
    </div>
  `;

  // Загрузить категории
  api.getCategories().then(cats => {
    const select = document.getElementById('categoryId');
    if (select) {
      select.innerHTML = cats.map(c => `<option value="${c.id}">${c.name}</option>`).join('');
    }
  });

  // Мини-карта
  const miniMap = L.map('miniMap').setView([55.7558, 37.6173], 10);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(miniMap);
  let marker;
  miniMap.on('click', e => {
    const { lat, lng } = e.latlng;
    if (marker) miniMap.removeLayer(marker);
    marker = L.marker([lat, lng]).addTo(miniMap);
    const geoJson = JSON.stringify({ type: 'Point', coordinates: [lng, lat] });
    document.getElementById('locationGeoJson').value = geoJson;
  });

  document.getElementById('appealForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const form = e.target;
    if (!form.locationGeoJson.value) {
      showToast('Укажите точку на карте', 'warning');
      return;
    }
    const formData = new FormData();
    formData.append('title', form.title.value);
    formData.append('description', form.description.value);
    formData.append('address', form.address.value);
    formData.append('locationGeoJson', form.locationGeoJson.value);
    formData.append('categoryId', form.categoryId.value);
    const files = form.photos.files;
    for (let i = 0; i < files.length; i++) {
      formData.append('photos', files[i]);
    }
    try {
      await api.createAppeal(formData);
      showToast('Обращение подано!', 'success');
      window.location.hash = '#/appeals';
    } catch (err) {
      showToast(err.message, 'danger');
    }
  });
}

async function adminPage() {
  const content = document.getElementById('mainContent');
  content.innerHTML = `<div class="card">Загрузка...</div>`;
  try {
    const [pending, categories, districts] = await Promise.all([
      api.getPendingDeputies(),
      api.getCategories(),
      api.getDistricts()
    ]);
    let html = `
      <div class="card">
        <h2>Админ-панель</h2>
        <h3>Неподтверждённые депутаты</h3>
        ${pending.length ? pending.map(d => `
          <div class="flex-between mb-10">
            <span>${d.fullName} (${d.email})</span>
            <select id="district-${d.id}" class="form-control" style="width:200px">
              <option value="">Выберите округ</option>
              ${districts.map(dd => `<option value="${dd.id}">${dd.name}</option>`).join('')}
            </select>
            <button class="btn btn-success approve-btn" data-id="${d.id}">Утвердить</button>
            <button class="btn btn-danger reject-btn" data-id="${d.id}">Отклонить</button>
          </div>
        `).join('') : '<p>Нет запросов</p>'}
        <h3 class="mt-20">Категории</h3>
        <button id="addCategoryBtn" class="btn btn-primary mb-10">Добавить категорию</button>
        <ul>${categories.map(c => `<li>${c.name} (${c.isActive ? 'активна' : 'неактивна'}) <button class="btn btn-sm btn-warning edit-cat" data-id="${c.id}">Ред</button> <button class="btn btn-sm btn-danger del-cat" data-id="${c.id}">Уд</button></li>`).join('')}</ul>
        <h3 class="mt-20">Округа</h3>
        <button id="addDistrictBtn" class="btn btn-primary mb-10">Добавить округ</button>
        <ul>${districts.map(d => `<li>${d.name} <button class="btn btn-sm btn-warning edit-dist" data-id="${d.id}">Ред</button> <button class="btn btn-sm btn-danger del-dist" data-id="${d.id}">Уд</button></li>`).join('')}</ul>
      </div>
    `;
    content.innerHTML = html;

    // Утверждение/отклонение депутатов
    document.querySelectorAll('.approve-btn').forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.dataset.id;
        const districtId = document.getElementById(`district-${id}`).value;
        if (!districtId) return showToast('Выберите округ', 'warning');
        try {
          await api.approveDeputy(id, true, parseInt(districtId));
          showToast('Депутат утверждён', 'success');
          adminPage();
        } catch (err) { showToast(err.message, 'danger'); }
      });
    });
    document.querySelectorAll('.reject-btn').forEach(btn => {
      btn.addEventListener('click', async () => {
        if (!confirm('Отклонить депутата?')) return;
        try {
          await api.approveDeputy(btn.dataset.id, false, null);
          showToast('Депутат отклонён', 'success');
          adminPage();
        } catch (err) { showToast(err.message, 'danger'); }
      });
    });

    // Добавление категории
    document.getElementById('addCategoryBtn').addEventListener('click', () => {
      openModal(`
        <h3>Новая категория</h3>
        <input id="catName" class="form-control" placeholder="Название">
        <input id="catDesc" class="form-control" placeholder="Описание">
        <label><input type="checkbox" id="catActive" checked> Активна</label>
        <button id="saveCatBtn" class="btn btn-primary mt-10">Сохранить</button>
      `);
      document.getElementById('saveCatBtn').addEventListener('click', async () => {
        const data = {
          name: document.getElementById('catName').value,
          description: document.getElementById('catDesc').value,
          isActive: document.getElementById('catActive').checked
        };
        try {
          await api.createCategory(data);
          showToast('Категория создана', 'success');
          closeModal();
          adminPage();
        } catch (err) { showToast(err.message, 'danger'); }
      });
    });

    // Удаление категорий
    document.querySelectorAll('.del-cat').forEach(btn => {
      btn.addEventListener('click', async () => {
        if (!confirm('Удалить категорию?')) return;
        try {
          await api.deleteCategory(btn.dataset.id);
          showToast('Категория удалена', 'success');
          adminPage();
        } catch (err) { showToast(err.message, 'danger'); }
      });
    });

    // Добавление округа (упрощённо, без рисования)
    document.getElementById('addDistrictBtn').addEventListener('click', () => {
      openModal(`
        <h3>Новый округ</h3>
        <input id="distName" class="form-control" placeholder="Название">
        <input id="distDesc" class="form-control" placeholder="Описание">
        <textarea id="distGeoJson" class="form-control" placeholder="GeoJSON полигона"></textarea>
        <button id="saveDistBtn" class="btn btn-primary mt-10">Сохранить</button>
      `);
      document.getElementById('saveDistBtn').addEventListener('click', async () => {
        const data = {
          name: document.getElementById('distName').value,
          description: document.getElementById('distDesc').value,
          boundaryGeoJson: document.getElementById('distGeoJson').value
        };
        try {
          await api.createDistrict(data);
          showToast('Округ создан', 'success');
          closeModal();
          adminPage();
        } catch (err) { showToast(err.message, 'danger'); }
      });
    });

    // Удаление округов
    document.querySelectorAll('.del-dist').forEach(btn => {
      btn.addEventListener('click', async () => {
        if (!confirm('Удалить округ?')) return;
        try {
          await api.deleteDistrict(btn.dataset.id);
          showToast('Округ удалён', 'success');
          adminPage();
        } catch (err) { showToast(err.message, 'danger'); }
      });
    });

  } catch (err) {
    content.innerHTML = `<div class="card">Ошибка: ${err.message}</div>`;
  }
}

async function deputyPage() {
  const content = document.getElementById('mainContent');
  content.innerHTML = `<div class="card">Загрузка...</div>`;
  try {
    const [appeals, stats] = await Promise.all([
      api.getAppeals({ pageSize: 100 }),
      api.getDeputyStats()
    ]);
    let html = `
      <div class="card">
        <h2>Кабинет депутата</h2>
        <div class="stats-grid">
          <div class="stat-item"><span class="stat-value">${stats.total}</span><span class="stat-label">Всего</span></div>
          <div class="stat-item"><span class="stat-value">${stats.byStatus?.find(s=>s.status===2)?.count || 0}</span><span class="stat-label">В работе</span></div>
          <div class="stat-item"><span class="stat-value">${stats.byStatus?.find(s=>s.status===3)?.count || 0}</span><span class="stat-label">Выполнено</span></div>
        </div>
        <h3>Обращения округа</h3>
        <div class="table-responsive">
          <table>
            <tr><th>ID</th><th>Заголовок</th><th>Статус</th><th>Дата</th><th></th></tr>
            ${appeals.items.map(a => `
              <tr>
                <td>${a.id}</td>
                <td>${a.title}</td>
                <td><span class="status status-${a.status}">${getStatusText(a.status)}</span></td>
                <td>${formatDate(a.createdAt)}</td>
                <td><a href="#/appeals/${a.id}" class="btn btn-sm btn-outline">Просмотр</a></td>
              </tr>
            `).join('')}
          </table>
        </div>
      </div>
    `;
    content.innerHTML = html;
  } catch (err) {
    content.innerHTML = `<div class="card">Ошибка: ${err.message}</div>`;
  }
}

function mapPage() {
  document.getElementById('mainContent').innerHTML = `
    <div class="card">
      <h2>Карта округов и обращений</h2>
      <div class="map-filters flex">
        <select id="filterCategory" class="form-control"><option value="">Все категории</option></select>
        <select id="filterStatus" class="form-control">
          <option value="">Все статусы</option>
          <option value="0">Новое</option>
          <option value="1">На рассмотрении</option>
          <option value="2">В работе</option>
          <option value="3">Выполнено</option>
          <option value="4">Отклонено</option>
        </select>
        <select id="filterDistrict" class="form-control"><option value="">Все округа</option></select>
        <button id="applyMapFilter" class="btn btn-primary">Применить</button>
      </div>
      <div id="map" class="map-container"></div>
    </div>
  `;

  // Загрузить категории и округа в фильтры
  api.getCategories().then(cats => {
    const sel = document.getElementById('filterCategory');
    cats.forEach(c => { sel.innerHTML += `<option value="${c.id}">${c.name}</option>`; });
  });
  api.getDistricts().then(dists => {
    const sel = document.getElementById('filterDistrict');
    dists.forEach(d => { sel.innerHTML += `<option value="${d.id}">${d.name}</option>`; });
  });

  initMap();
  document.getElementById('applyMapFilter').addEventListener('click', () => {
    const filters = {
      categoryId: document.getElementById('filterCategory').value,
      status: document.getElementById('filterStatus').value,
      districtId: document.getElementById('filterDistrict').value
    };
    loadAppealsOnMap(filters);
  });
}

function initMap() {
  if (currentMap) {
    currentMap.remove();
  }
  currentMap = L.map('map').setView([55.7558, 37.6173], 10);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap'
  }).addTo(currentMap);

  // Границы округов
  api.getDistrictsGeoJson().then(data => {
    L.geoJSON(data, {
      style: { color: '#1e3a8a', weight: 2, fillOpacity: 0.1 }
    }).addTo(currentMap);
  });

  loadAppealsOnMap();
}

async function loadAppealsOnMap(filters = {}) {
  if (mapLayers.appeals) {
    currentMap.removeLayer(mapLayers.appeals);
  }
  try {
    const data = await api.getAppealsGeoJson(filters);
    mapLayers.appeals = L.geoJSON(data, {
      pointToLayer: (feature, latlng) => {
        return L.circleMarker(latlng, {
          radius: 8,
          fillColor: getStatusColor(feature.properties.status),
          color: '#000',
          weight: 1,
          opacity: 1,
          fillOpacity: 0.8
        });
      },
      onEachFeature: (feature, layer) => {
        layer.bindPopup(`
          <b>${feature.properties.title}</b><br>
          Статус: ${getStatusText(feature.properties.status)}<br>
          Категория: ${feature.properties.category}<br>
          <a href="#/appeals/${feature.properties.id}">Подробнее</a>
        `);
      }
    }).addTo(currentMap);
  } catch (err) {
    showToast('Ошибка загрузки обращений', 'danger');
  }
}

function getStatusColor(status) {
  const colors = ['#94a3b8', '#3498db', '#f39c12', '#10b981', '#ef4444'];
  return colors[status] || '#94a3b8';
}

function notFoundPage() {
  document.getElementById('mainContent').innerHTML = `<div class="card"><h2>404 - Страница не найдена</h2></div>`;
}

// Запуск роутера
router.handleRoute();