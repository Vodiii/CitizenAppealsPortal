export function formatDate(isoString) {
    if (!isoString) return '';
    return new Date(isoString).toLocaleString('ru-RU');
  }
  
  export function getStatusText(status) {
    const map = ['Новое', 'На рассмотрении', 'В работе', 'Выполнено', 'Отклонено'];
    return map[status] || 'Неизвестно';
  }
  
  export function showToast(message, type = 'info') {
    const toast = document.getElementById('toast');
    toast.textContent = message;
    toast.className = `toast show ${type}`;
    setTimeout(() => toast.classList.remove('show'), 3000);
  }
  
  export function closeModal() {
    document.getElementById('modal').style.display = 'none';
  }
  
  export function openModal(content) {
    document.getElementById('modalBody').innerHTML = content;
    document.getElementById('modal').style.display = 'block';
    document.querySelector('.close').addEventListener('click', closeModal);
    window.addEventListener('click', (e) => {
      if (e.target === document.getElementById('modal')) closeModal();
    });
  }
  
  export function renderPagination(currentPage, totalPages, onPageChange) {
    if (totalPages <= 1) return '';
    let html = '<div class="pagination flex gap-10 mt-20">';
    for (let i = 1; i <= totalPages; i++) {
      html += `<button class="btn ${i === currentPage ? 'btn-primary' : 'btn-light'}" data-page="${i}">${i}</button>`;
    }
    html += '</div>';
    const container = document.createElement('div');
    container.innerHTML = html;
    container.querySelectorAll('button').forEach(btn => {
      btn.addEventListener('click', () => onPageChange(parseInt(btn.dataset.page)));
    });
    return container.innerHTML;
  }
  