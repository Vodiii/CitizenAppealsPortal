Ссылка на яндекс доску с задачами : https://boards.yandex.ru/whiteboard/?hash=486b7d5b2ce99b0a55d8a8e569aa5f9d

🏛️ Портал обращений граждан к депутатам
Веб-приложение, которое связывает жителей с местными депутатами: граждане подают обращения, указывая проблему на карте, а депутаты обрабатывают их в своих округах. Система автоматически определяет депутата по координатам, поддерживает голосование, комментарии и уведомления в реальном времени (SignalR).

📋 Основные возможности
Три роли: гражданин, депутат, администратор
Подача обращения: описание, категория, фотографии, выбор точки на карте (Яндекс.Карты с геокодированием)
Личный кабинет гражданина: отслеживание статуса, ответы депутата, возможность возобновить обращение
Кабинет депутата: список обращений округа (сортировка по рейтингу), смена статуса, ответ гражданину, статистика
Админ-панель: управление округами (GeoJSON-полигоны), категориями, утверждение/отклонение депутатов, установка срока полномочий
Интерактивная карта с границами округов и точками обращений (Leaflet)
Голосование за обращения (👍/👎) с автоматической сортировкой для депутатов
Комментарии к обращениям с правами на редактирование/удаление
Мгновенные уведомления через SignalR при смене статуса, новых ответах, голосах
🛠️ Технологический стек
Бэкенд:

ASP.NET Core 8 (REST API)
Entity Framework Core 8
PostgreSQL 16 + PostGIS (геоданные)
NetTopologySuite (GeoJSON)
JWT-аутентификация (ASP.NET Core Identity)
SignalR (реактивные уведомления)
Swagger (OpenAPI)
Фронтенд:

Чистый JavaScript (ES6 модули)
Leaflet (главная карта)
Яндекс.Карты (мини-карта при подаче обращения)
SignalR клиент
HTML5 / CSS3 (адаптивный дизайн)
📁 Структура репозитория
CitizenAppealsPortal/ ├── Controllers/ # API контроллеры ├── Data/ # ApplicationDbContext и миграции ├── Hubs/ # SignalR хаб (NotificationHub) ├── Models/ # Сущности БД и DTO ├── Services/ # GeoService, FileService ├── wwwroot/uploads/ # Загруженные фотографии ├── frontend/ # Клиентская часть │ ├── index.html │ ├── css/ │ └── js/ # app.js, api.js, auth.js, router.js, utils.js ├── sql/ # SQL-скрипты (фиксы, утилиты) ├── Program.cs # Точка входа и конфигурация ├── appsettings.json # Настройки подключения к БД и JWT └── CitizenAppealsPortal.csproj

🚀 Быстрый старт (локальная разработка)
Требования
.NET 8 SDK
PostgreSQL (16+)
PostGIS (расширение)
Node.js (для Live Server) или VS Code с расширением Live Server
1. Клонирование репозитория
git clone https://github.com/your-username/CitizenAppealsPortal.git cd CitizenAppealsPortal

Настройка базы данных Создайте пустую базу данных CitizenAppealsDb в PostgreSQL.
Активируйте расширение PostGIS:

sql CREATE EXTENSION postgis; Восстановите дамп базы (если есть) или примените миграции:

bash dotnet ef database update 3. Конфигурация Откройте appsettings.json и укажите строку подключения к вашей БД:

json "DefaultConnection": "Host=localhost;Port=5432;Database=CitizenAppealsDb;Username=postgres;Password=ваш_пароль" При необходимости измените JWT-ключ и учётные данные администратора.

Запуск бэкенда bash dotnet run API будет доступен по адресу http://localhost:5000, Swagger — http://localhost:5000/swagger.

Запуск фронтенда Откройте папку frontend в VS Code, нажмите правой кнопкой по index.html → Open with Live Server. Или через командную строку:

bash npx serve frontend 📖 Документация API Полная интерактивная документация (Swagger) доступна после запуска бэкенда по адресу /swagger. Для защищённых эндпоинтов необходимо получить JWT-токен через POST /api/Auth/login и передавать его в заголовке Authorization: Bearer <токен>.

🧪 Тестовые учётные данные Роль Email Пароль Администратор admin@example.com Admin123! Депутат dep1@mail.ru Dep123! Гражданин citizen1@mail.ru Citizen123! 🔧 Возможные проблемы и решения Ошибка CORS: убедитесь, что в Program.cs разрешён origin вашего фронтенда.

Ошибка 401 (SignalR): проверьте, что accessTokenFactory возвращает актуальный токен.

Геоданные не сохраняются: проверьте, что в БД активировано расширение PostGIS.

Категории не отображаются: выполните SELECT * FROM "Categories"; – если таблица пуста, создайте категории через Swagger (POST /api/admin/categories).

📌 Лицензия Проект создан в рамках учебного курса/хакатона. Использование в личных и образовательных целях без ограничений.
