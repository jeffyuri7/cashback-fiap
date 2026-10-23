// Painel do curso: chama a API do app de cashback direto do navegador.
// Da aula 2 em diante, só a API Clean (o projeto que evolui).
// Aba "App": o que o usuário do app vê. Aba "Loja parceira": o HTTP como ele é (método, headers, status, corpo).
// Aula 3: o painel fala com o gateway (:5100), que divide o tráfego entre a versão estável e o canário.
// A porta pode vir no endereço (?api=5204), para ensaiar numa cópia com outras portas.
const API = { nome: 'gateway', porta: Number(new URLSearchParams(location.search).get('api') ?? 5100) };
const VERSOES = { estavel: 'estável', canario: 'canário', local: 'local' };

const CATEGORIAS = { supermarket: 'Supermercado', fuel: 'Combustível', pharmacy: 'Farmácia', books: 'Livraria' };

const $ = (sel, raiz = document) => raiz.querySelector(sel);
const $$ = (sel, raiz = document) => [...raiz.querySelectorAll(sel)];
const real = (v) => Number(v).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
const pct = (v) => `${(Number(v) * 100).toLocaleString('pt-BR', { maximumFractionDigits: 2 })}%`;
const esc = (s) => String(s).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);

let clienteExtrato = 'c-001';
let lojas = [];
let clientes = [];

async function chamar(api, metodo, caminho, corpo) {
  try {
    const r = await fetch(`http://localhost:${api.porta}${caminho}`, {
      method: metodo,
      headers: corpo ? { 'Content-Type': 'application/json' } : {},
      body: corpo ? JSON.stringify(corpo) : undefined,
    });
    const texto = await r.text();
    let dados = null;
    try { dados = texto ? JSON.parse(texto) : null; } catch { /* o gateway responde HTML quando a versão está fora do ar */ }
    return { ok: r.ok, status: r.status, dados, versao: r.headers.get('X-Versao') };
  } catch {
    return { ok: false, status: 0, dados: null };
  }
}

const erroTexto = (res) =>
  res.status === 0 ? 'API fora do ar' :
  res.status === 404 ? 'Cliente não encontrado (404)' :
  (res.dados?.errors ?? ['Erro']).join(' ');

// ───────────── status da API ─────────────
async function atualizarStatus() {
  const res = await chamar(API, 'GET', '/stores');
  $('#status').innerHTML = `<li class="${res.ok ? 'on' : 'off'}" title="localhost:${API.porta}">API Clean via ${API.nome} :${API.porta}</li>`;
  return res.ok;
}

// ───────────── aba "App" ─────────────
async function carregarCatalogo() {
  const [l, c] = await Promise.all([chamar(API, 'GET', '/stores'), chamar(API, 'GET', '/customers')]);
  if (!l.ok || !c.ok) return false;
  lojas = l.dados;
  clientes = c.dados;
  $$('.sel-loja').forEach((s) => {
    const atual = s.value;
    s.innerHTML = lojas.map((l) => `<option value="${l.id}">${esc(l.name)} · ${CATEGORIAS[l.category] ?? l.category}</option>`).join('');
    if (atual) s.value = atual;
  });
  $$('.sel-cliente').forEach((s) => {
    const atual = s.value;
    s.innerHTML = clientes.map((c) => `<option value="${c.id}">${esc(c.name)}</option>`).join('');
    if (atual) s.value = atual;
  });
  return true;
}

async function carregarApp() {
  const res = await chamar(API, 'GET', '/customers');
  const caixa = $('#clientes');
  if (!res.ok) {
    caixa.innerHTML = `<p class="erro">${erroTexto(res)} (localhost:${API.porta})</p>`;
    $('#extrato').innerHTML = '';
    return;
  }
  const saldos = await Promise.all(res.dados.map((c) => chamar(API, 'GET', `/customers/${c.id}/balance`)));
  caixa.innerHTML = res.dados.map((c, i) => {
    const b = saldos[i].dados ?? {};
    const uso = b.monthlyLimit ? Math.min(100, (b.creditedThisMonth / b.monthlyLimit) * 100) : 0;
    return `<button class="cliente ${c.id === clienteExtrato ? 'sel' : ''}" data-id="${c.id}">
        <strong>${esc(c.name)}</strong>
        <span class="saldo">${real(c.balance)}</span>
        <span class="teto"><span style="width:${uso}%"></span></span>
        <small>${real(b.creditedThisMonth ?? 0)} de ${real(b.monthlyLimit ?? 0)} creditados no mês</small>
      </button>`;
  }).join('');
  $$('#clientes .cliente').forEach((b) => b.onclick = () => {
    clienteExtrato = b.dataset.id;
    carregarApp();
  });
  carregarExtrato();
}

async function carregarExtrato() {
  const nome = clientes.find((c) => c.id === clienteExtrato)?.name ?? clienteExtrato;
  $('#extrato-quem').textContent = `· ${nome}`;
  const res = await chamar(API, 'GET', `/customers/${clienteExtrato}/purchases`);
  $('#extrato').innerHTML = !res.ok ? `<tr><td colspan="6" class="erro">${erroTexto(res)}</td></tr>` :
    res.dados.length === 0 ? '<tr><td colspan="6" class="vazio">Nenhuma compra ainda.</td></tr>' :
    res.dados.map((p) => `<tr>
        <td>${new Date(p.createdAt).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'medium' })}</td>
        <td>${esc(p.storeName)}</td><td class="num">${real(p.amount)}</td>
        <td class="num">${pct(p.percentage)}</td><td class="num forte">${real(p.cashback)}</td>
        <td class="num">${p.campaignBonus ? real(p.campaignBonus) : '—'}</td></tr>`).join('');
}

$('#form-compra').onsubmit = async (e) => {
  e.preventDefault();
  const f = new FormData(e.target);
  const corpo = { customerId: f.get('cliente'), storeId: f.get('loja'), amount: Number(f.get('valor')) };
  const res = await chamar(API, 'POST', '/purchases', corpo);
  $('#resultado-compra').innerHTML = res.ok ? `
      <div class="ok">
        <div><small>categoria</small><strong>${CATEGORIAS[res.dados.category] ?? res.dados.category}</strong></div>
        <div><small>percentual</small><strong>${pct(res.dados.percentage)}</strong></div>
        <div><small>cashback creditado</small><strong class="destaque">${real(res.dados.cashback)}</strong>
          ${res.dados.campaignBonus ? `<small>inclui ${real(res.dados.campaignBonus)} da Black Friday</small>` : ''}</div>
        <div><small>saldo agora</small><strong>${real(res.dados.balance)}</strong></div>
        <div><small>quem atendeu</small><strong class="versao ${res.versao}">${VERSOES[res.versao] ?? res.versao ?? '?'}</strong></div>
      </div>` : `<p class="erro">${erroTexto(res)}${res.versao ? ` · versão ${VERSOES[res.versao] ?? res.versao}` : ''}</p>`;
  clienteExtrato = corpo.customerId;
  carregarApp();
};

$('#form-resgate').onsubmit = async (e) => {
  e.preventDefault();
  const f = new FormData(e.target);
  const id = f.get('cliente');
  const res = await chamar(API, 'POST', `/customers/${id}/redemptions`, { amount: Number(f.get('valor')) });
  $('#resultado-resgate').innerHTML = res.ok
    ? `<p class="ok-linha">PIX enviado para <strong>${esc(res.dados.pixKey)}</strong> · saldo agora <strong>${real(res.dados.balance)}</strong></p>`
    : `<p class="erro">${erroTexto(res)}</p>`;
  carregarApp();
};

// ───────────── aba "loja parceira" (aula 2): o HTTP como ele é ─────────────
const MOTIVO = { 0: 'API fora do ar', 200: 'OK', 201: 'Created', 204: 'No Content', 400: 'Bad Request', 404: 'Not Found',
  409: 'Conflict', 415: 'Unsupported Media Type', 422: 'Unprocessable Content', 500: 'Internal Server Error' };
// headers de infraestrutura que não interessam à aula
const HEADERS_OCULTOS = /^(date|server|content-length|transfer-encoding|vary|connection|keep-alive|access-control-.*)$/i;
let ultimaRequisicao = null;
let tentativas = [];

const formLoja = $('#form-loja');
const campo = (nome) => formLoja.elements[nome];

function montarCorpo() {
  campo('corpo').value = JSON.stringify(
    { customerId: campo('cliente').value, storeId: campo('loja').value, amount: Number(campo('valor').value) }, null, 2);
}
['cliente', 'loja', 'valor'].forEach((n) => campo(n).addEventListener('input', montarCorpo));
$('#nova-chave').onclick = () => { campo('chave').value = crypto.randomUUID(); campo('usarChave').checked = true; };
campo('chave').value = crypto.randomUUID();

async function enviarBruto(req) {
  const headers = { 'Content-Type': 'application/json', ...req.headers };
  try {
    const r = await fetch(`http://localhost:${API.porta}${req.caminho}`, { method: 'POST', headers, body: req.corpo });
    const texto = await r.text();
    const recebidos = [...r.headers.entries()].filter(([k]) => !HEADERS_OCULTOS.test(k));
    return { status: r.status, headers: recebidos, texto };
  } catch {
    return { status: 0, headers: [], texto: '' };
  }
}

function desenharRequisicao(req) {
  const linhas = [`<span class="linha1">POST ${esc(req.caminho)} HTTP/1.1</span>`, '<span class="h">Content-Type</span>: application/json'];
  Object.entries(req.headers).forEach(([k, v]) => linhas.push(`<span class="h">${esc(k)}</span>: ${esc(v)}`));
  $('#http-req').innerHTML = `${linhas.join('\n')}\n\n${esc(req.corpo)}`;
}

function desenharResposta(res, perdida) {
  if (perdida) {
    $('#http-res').innerHTML = '<span class="aviso">⏳ Timeout: a resposta não chegou.\nA compra foi registrada? A loja não tem como saber.</span>';
    return;
  }
  const classe = `s${String(res.status)[0]}`;
  const linhas = [`<span class="linha1 ${classe}">HTTP ${res.status} ${MOTIVO[res.status] ?? ''}</span>`];
  res.headers.forEach(([k, v]) => linhas.push(`<span class="h">${esc(k)}</span>: ${esc(v)}`));
  let corpo = res.texto;
  try { corpo = JSON.stringify(JSON.parse(res.texto), null, 2); } catch { /* corpo vazio ou não-JSON */ }
  $('#http-res').innerHTML = `${linhas.join('\n')}${corpo ? `\n\n${esc(corpo)}` : ''}`;
}

function desenharTentativas() {
  $('#tentativas').innerHTML = tentativas.map((t) => `<li>${t}</li>`).join('');
}

async function enviarLoja(req, simularTimeout) {
  ultimaRequisicao = req;
  $('#reenviar').disabled = false;
  desenharRequisicao(req);
  const res = await enviarBruto(req);
  desenharResposta(res, simularTimeout && res.status !== 0);
  const n = tentativas.length + 1;
  const replay = res.headers.some(([k, v]) => k.toLowerCase() === 'idempotent-replayed' && v === 'true');
  tentativas.push(simularTimeout && res.status !== 0
    ? `${n}º envio · ⏳ timeout: a loja não sabe se a compra passou`
    : `${n}º envio · ${res.status} ${MOTIVO[res.status] ?? ''}${replay ? ' · resposta repetida (Idempotent-Replayed)' : ''}`);
  desenharTentativas();
  carregarExtratoLoja();
}

formLoja.onsubmit = (e) => {
  e.preventDefault();
  const headers = campo('usarChave').checked && campo('chave').value ? { 'Idempotency-Key': campo('chave').value } : {};
  tentativas = [];
  enviarLoja({ caminho: campo('versao').value, headers, corpo: campo('corpo').value }, campo('timeout').checked);
};
$('#reenviar').onclick = () => ultimaRequisicao && enviarLoja(ultimaRequisicao, false);

async function carregarExtratoLoja() {
  let id = campo('cliente').value;
  try { id = JSON.parse(campo('corpo').value).customerId ?? id; } catch { /* corpo inválido: usa o seletor */ }
  const nome = clientes.find((c) => c.id === id)?.name ?? id;
  const [compras, saldo] = await Promise.all([
    chamar(API, 'GET', `/customers/${id}/purchases`), chamar(API, 'GET', `/customers/${id}/balance`)]);
  $('#loja-quem').textContent = `· ${nome}${saldo.ok ? ` · saldo ${real(saldo.dados.balance)}` : ''}`;
  $('#loja-extrato').innerHTML = !compras.ok ? `<tr><td colspan="6" class="erro">${erroTexto(compras)}</td></tr>` :
    compras.dados.length === 0 ? '<tr><td colspan="6" class="vazio">Nenhuma compra ainda.</td></tr>' :
    compras.dados.map((p) => `<tr>
        <td>${new Date(p.createdAt).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'medium' })}</td>
        <td>${esc(p.storeName)}</td><td class="num">${real(p.amount)}</td>
        <td class="num">${pct(p.percentage)}</td><td class="num forte">${real(p.cashback)}</td>
        <td class="num">${p.campaignBonus ? real(p.campaignBonus) : '—'}</td></tr>`).join('');
}

// ───────────── aba "Operação" (aula 3): feature flags ─────────────
const ESTADOS = [
  { valor: 'off', rotulo: 'Desligada' },
  { valor: 'test', rotulo: 'Grupo de teste' },
  { valor: 'on', rotulo: 'Todos, no horário' },
];
const PUBLICO = { none: 'ninguém', 'test-group': 'só o grupo de teste', everyone: 'todos' };
const dataHora = (iso) => new Date(iso).toLocaleString('pt-BR', { ...HORA, second: undefined });

async function carregarFlags() {
  const res = await chamar(API, 'GET', '/feature-flags');
  const caixa = $('#flags');
  if (!res.ok) {
    caixa.innerHTML = `<p class="erro">${res.status === 404 ? 'Esta versão da API ainda não tem feature flags.' : erroTexto(res)}</p>`;
    return;
  }
  caixa.innerHTML = res.dados.map((f) => {
    const grupo = f.testCustomers.map((id) => clientes.find((c) => c.id === id)?.name ?? id).join(', ');
    // o fim é exclusivo (00h00 de segunda); para ler, mostra o último minuto da campanha
    const fim = f.endsAt ? dataHora(new Date(new Date(f.endsAt).getTime() - 60000)) : 'sem fim';
    return `<div class="flag">
        <div class="flag-nome"><code>${esc(f.name)}</code><span>${esc(f.description)}</span></div>
        <div class="flag-estados" role="group">${ESTADOS.map((e) =>
          `<button data-flag="${esc(f.name)}" data-estado="${e.valor}" class="${f.state === e.valor ? 'ativo' : ''}">${e.rotulo}</button>`).join('')}
        </div>
        <dl class="flag-dados">
          <dt>Vale agora para</dt><dd class="publico ${f.audienceNow}">${PUBLICO[f.audienceNow]}</dd>
          <dt>Horário (todos)</dt><dd>${f.startsAt ? dataHora(f.startsAt) : 'sem início'} → ${fim}</dd>
          <dt>Grupo de teste</dt><dd>${esc(grupo || 'ninguém')}</dd>
          <dt>Última mudança</dt><dd>${f.changedAt ? `${esc(f.changedBy)}, ${dataHora(f.changedAt)}` : 'nunca mudou'}</dd>
        </dl>
      </div>`;
  }).join('');
  $$('#flags [data-estado]').forEach((b) => b.onclick = async () => {
    await fetch(`http://localhost:${API.porta}/feature-flags/${b.dataset.flag}`, {
      method: 'PUT', headers: { 'Content-Type': 'application/json', 'X-Operador': 'painel' },
      body: JSON.stringify({ state: b.dataset.estado }),
    }).catch(() => null);
    carregarFlags();
  });
}

// ───────────── aba "Operação" (aula 3): o canário ─────────────
// Uma rajada de compras iguais pelo gateway: quantas cada versão atendeu, quantas deram erro e o percentual de cada uma.
$('#form-rajada').onsubmit = async (e) => {
  e.preventDefault();
  const f = new FormData(e.target);
  const corpo = { customerId: f.get('cliente'), storeId: f.get('loja'), amount: Number(f.get('valor')) };
  const contagem = {};
  $('#rajada-resultado').innerHTML = '<p class="vazio">Enviando...</p>';
  for (let i = 0; i < Number(f.get('quantas')); i++) {
    const res = await chamar(API, 'POST', '/purchases', corpo);
    const v = res.versao ?? (res.status === 0 ? 'fora do ar' : 'sem versão');
    const c = contagem[v] ??= { total: 0, erros: 0, pcts: new Set() };
    c.total++;
    if (res.ok) c.pcts.add(pct(res.dados.percentage)); else c.erros++;
  }
  $('#rajada-resultado').innerHTML = `<table class="tabela">
      <thead><tr><th>Versão</th><th class="num">Respostas</th><th class="num">Erros</th><th>Percentual dado</th></tr></thead>
      <tbody>${Object.entries(contagem).map(([v, c]) => `<tr><td><strong class="versao ${v}">${VERSOES[v] ?? v}</strong></td>
        <td class="num">${c.total}</td><td class="num ${c.erros ? 'erro' : ''}">${c.erros}</td><td>${[...c.pcts].join(', ') || '—'}</td></tr>`).join('')}
      </tbody></table>`;
  carregarApp();
};

// ───────────── relógio do app (aula 3) ─────────────
// A API diz que horas são para ela; o painel faz o relógio andar entre uma consulta e outra.
let relogio = null; // { app: hora do app, local: hora do navegador na consulta, ajustado }
const HORA = { weekday: 'short', day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit',
  timeZone: 'America/Sao_Paulo' };

async function lerRelogio() {
  const res = await chamar(API, 'GET', '/demo/clock');
  relogio = res.ok ? { app: new Date(res.dados.now), local: new Date(), ajustado: res.dados.adjusted } : null;
  desenharRelogio();
}

function desenharRelogio() {
  const hora = $('#relogio-hora');
  if (!relogio) { hora.textContent = '—'; return; }
  const agora = new Date(relogio.app.getTime() + (Date.now() - relogio.local.getTime()));
  hora.textContent = agora.toLocaleString('pt-BR', HORA);
  hora.classList.toggle('ajustado', relogio.ajustado);
}

$('#relogio-pular').onchange = async (e) => {
  const alvo = e.target.value;
  await (alvo ? chamar(API, 'PUT', '/demo/clock', { now: alvo }) : chamar(API, 'DELETE', '/demo/clock'));
  await lerRelogio();
  carregarApp();
  if (!$('#aba-operacao').classList.contains('escondido')) carregarFlags();
};
setInterval(desenharRelogio, 1000);

// ───────────── abas e início ─────────────
$$('.aba').forEach((b) => b.onclick = () => {
  $$('.aba').forEach((x) => x.classList.toggle('ativa', x === b));
  $('#aba-app').classList.toggle('escondido', b.dataset.aba !== 'app');
  $('#aba-loja').classList.toggle('escondido', b.dataset.aba !== 'loja');
  $('#aba-operacao').classList.toggle('escondido', b.dataset.aba !== 'operacao');
  if (b.dataset.aba === 'app') carregarApp();
  else if (b.dataset.aba === 'operacao') carregarFlags();
  else { if (!campo('corpo').value) montarCorpo(); carregarExtratoLoja(); }
});

// Se a API ainda não subiu, o catálogo (lojas e clientes) é carregado quando ela voltar.
let catalogoOk = false;
async function verificar() {
  const no_ar = await atualizarStatus();
  lerRelogio();
  if (no_ar && !catalogoOk) {
    catalogoOk = await carregarCatalogo();
    if (catalogoOk) carregarApp();
  }
}

(async function iniciar() {
  await verificar();
  if (!catalogoOk) carregarApp();
  setInterval(verificar, 4000);
})();
