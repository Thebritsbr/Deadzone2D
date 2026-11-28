const links = document.querySelectorAll('.tablink');
for (const link of links) {
  link.addEventListener('click', () => {
    document.querySelectorAll('.tablink').forEach(l => l.classList.remove('active'));
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    link.classList.add('active');
    document.getElementById(link.dataset.tab).classList.add('active');
  });
}
fetch('update.json').then(r => r.json()).then(d => {
  document.getElementById('version').textContent = d.Version || '4.5.4';
  document.getElementById('latest').textContent = d.Version || '—';
  document.getElementById('notes').textContent = d.Notes || 'No notes available.';
}).catch(() => {
  document.getElementById('latest').textContent = '4.5.4';
  document.getElementById('notes').textContent = 'Check back soon.';
});
