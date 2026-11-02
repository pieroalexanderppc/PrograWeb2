/* Concreta: interacciones puntuales (vanilla JS) */
(function () {
  'use strict';

  // Toast de TempData
  document.querySelectorAll('[data-auto-toast]').forEach(function (el) {
    bootstrap.Toast.getOrCreateInstance(el).show();
  });

  // Modal de confirmación de eliminación: botones con data-confirmar
  var modal = document.getElementById('modalConfirmar');
  if (modal) {
    modal.addEventListener('show.bs.modal', function (ev) {
      var origen = ev.relatedTarget;
      modal.querySelector('form').setAttribute('action', origen.getAttribute('data-accion'));
      modal.querySelector('[data-mensaje]').textContent = origen.getAttribute('data-confirmar');
    });
  }

  // Propiedades del material elegido en un select: textos en <script type="application/json" id="propsMateriales">
  var fuenteProps = document.getElementById('propsMateriales');
  var props = fuenteProps ? JSON.parse(fuenteProps.textContent) : {};
  function mostrarProps(select) {
    var destino = document.getElementById(select.getAttribute('data-props-destino'));
    if (!destino) return;
    destino.textContent = props[select.value] || '';
  }
  document.querySelectorAll('select[data-props-destino]').forEach(function (s) {
    mostrarProps(s);
    s.addEventListener('change', function () { mostrarProps(s); });
  });

  // Mostrar u ocultar bloques según el valor de un select: data-mostrar-si="idSelect:valor1|valor2"
  var condicionales = document.querySelectorAll('[data-mostrar-si]');
  function actualizarCondicionales() {
    condicionales.forEach(function (el) {
      var partes = el.getAttribute('data-mostrar-si').split(':');
      var sel = document.getElementById(partes[0]);
      if (!sel) return;
      var texto = sel.options[sel.selectedIndex] ? sel.options[sel.selectedIndex].text : '';
      var valor = sel.value;
      var visibles = partes[1].split('|');
      var mostrar = visibles.indexOf(texto) >= 0 || visibles.indexOf(valor) >= 0 || (visibles.indexOf('*') >= 0 && valor !== '');
      el.hidden = !mostrar;
    });
  }
  if (condicionales.length) {
    var ids = {};
    condicionales.forEach(function (el) { ids[el.getAttribute('data-mostrar-si').split(':')[0]] = true; });
    Object.keys(ids).forEach(function (id) {
      var sel = document.getElementById(id);
      if (sel) sel.addEventListener('change', actualizarCondicionales);
    });
    actualizarCondicionales();
  }

  // Comparar: máximo 4 diseños marcados
  var checks = document.querySelectorAll('input[data-max-sel]');
  function limitar() {
    var max = parseInt(checks[0].getAttribute('data-max-sel'), 10);
    var marcados = Array.prototype.filter.call(checks, function (c) { return c.checked; }).length;
    checks.forEach(function (c) { c.disabled = !c.checked && marcados >= max; });
    var contador = document.getElementById('contadorSel');
    if (contador) contador.textContent = marcados + ' de ' + max + ' seleccionados';
  }
  if (checks.length) {
    checks.forEach(function (c) { c.addEventListener('change', limitar); });
    limitar();
  }

  // Nuevo diseño: si cambian los datos después de calcular, el resultado queda desactualizado
  var formCalculo = document.querySelector('form[data-calculo]');
  var panel = document.querySelector('[data-resultado]');
  if (formCalculo && panel) {
    var marcarDesactualizado = function (ev) {
      if (panel.contains(ev.target)) return;
      panel.classList.add('stale');
      var aviso = panel.querySelector('[data-aviso-stale]');
      if (aviso) aviso.hidden = false;
      panel.querySelectorAll('[data-guardar]').forEach(function (b) { b.disabled = true; });
    };
    formCalculo.addEventListener('input', marcarDesactualizado);
    formCalculo.addEventListener('change', marcarDesactualizado);
  }

  // Barra de composición: anima el ancho de los segmentos al aparecer
  document.querySelectorAll('.mix-bar[data-animar] > span').forEach(function (s) {
    var ancho = s.style.width;
    s.style.width = '0';
    requestAnimationFrame(function () { requestAnimationFrame(function () { s.style.width = ancho; }); });
  });
})();
