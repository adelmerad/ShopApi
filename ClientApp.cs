namespace ShopApi;

// Petite interface cliente (SPA) servie sur GET /app.
// Elle se connecte au serveur d'auth via Authorization Code + PKCE (SSO),
// puis appelle ShopApi (même origine) pour lister/ajouter des produits.
public static class ClientApp
{
    public const string Html =
"""
<!doctype html>
<html lang="fr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Boutique — démo SSO</title>
<style>
  :root{--bg:#f4f6f9;--surface:#fff;--ink:#151922;--muted:#5a6472;--border:#e1e6ee;--accent:#2f56d9;--teal:#0d8b82;--good:#1a7f4b;--err:#c0293b;}
  *{box-sizing:border-box;}
  body{margin:0;font-family:system-ui,-apple-system,"Segoe UI",Roboto,sans-serif;background:var(--bg);color:var(--ink);}
  header{background:var(--surface);border-bottom:1px solid var(--border);padding:14px 24px;display:flex;align-items:center;justify-content:space-between;gap:16px;flex-wrap:wrap;}
  header h1{font-size:18px;margin:0;}
  header .sub{font-size:12px;color:var(--muted);margin-top:2px;}
  .auth{display:flex;align-items:center;gap:10px;font-size:14px;}
  main{max-width:820px;margin:0 auto;padding:24px;display:flex;flex-direction:column;gap:22px;}
  .card{background:var(--surface);border:1px solid var(--border);border-radius:12px;padding:18px 20px;box-shadow:0 1px 2px rgba(20,25,40,.04),0 6px 20px rgba(20,25,40,.05);}
  .card h2{margin:0 0 14px;font-size:15px;display:flex;align-items:center;gap:8px;}
  button{padding:9px 14px;border:0;border-radius:8px;background:var(--accent);color:#fff;font-size:14px;cursor:pointer;}
  button.ghost{background:transparent;color:var(--accent);border:1px solid var(--border);}
  button:disabled{opacity:.5;cursor:not-allowed;}
  input,select{padding:9px;border:1px solid var(--border);border-radius:8px;font-size:14px;width:100%;background:var(--surface);color:var(--ink);}
  .row{display:flex;gap:10px;flex-wrap:wrap;align-items:end;}
  .field{display:flex;flex-direction:column;gap:5px;flex:1;min-width:130px;}
  .field label{font-size:12px;color:var(--muted);}
  table{width:100%;border-collapse:collapse;font-size:14px;}
  th,td{text-align:left;padding:9px 10px;border-bottom:1px solid var(--border);}
  th{font-size:11px;text-transform:uppercase;letter-spacing:.05em;color:var(--muted);}
  .badge{font-size:12px;padding:3px 9px;border-radius:999px;background:#eef1f7;color:var(--muted);}
  .badge.on{background:#dbf1e4;color:var(--good);}
  .msg{font-size:13px;padding:8px 12px;border-radius:8px;margin-top:12px;display:none;}
  .msg.ok{background:#dbf1e4;color:var(--good);display:block;}
  .msg.ko{background:#fdeaec;color:var(--err);display:block;}
  .locked{color:var(--muted);font-size:13px;}
</style>
</head>
<body>
<header>
  <div><h1>🛍️ Boutique — démo SSO</h1><div class="sub">Client → AuthApiTest (login) → ShopApi (données)</div></div>
  <div class="auth" id="authArea"></div>
</header>
<main>
  <div class="card">
    <h2>Produits</h2>
    <table>
      <thead><tr><th>Id</th><th>Nom</th><th>Prix</th><th>Catégorie</th></tr></thead>
      <tbody id="productsBody"><tr><td colspan="4" class="locked">Chargement…</td></tr></tbody>
    </table>
  </div>

  <div class="card">
    <h2>Ajouter <span class="badge">🔒 connexion requise</span></h2>
    <div id="addLocked" class="locked">Connecte-toi pour ajouter un produit ou une catégorie.</div>
    <div id="addForm" style="display:none;">
      <div class="row">
        <div class="field"><label>Nom du produit</label><input id="pName" placeholder="Clavier"></div>
        <div class="field"><label>Prix (€)</label><input id="pPrice" type="number" step="0.01" placeholder="49.90"></div>
        <div class="field"><label>Catégorie</label><select id="pCat"></select></div>
        <div class="field" style="flex:0;"><label>&nbsp;</label><button onclick="addProduct()">Ajouter</button></div>
      </div>
      <div class="row" style="margin-top:12px;">
        <div class="field"><label>Nouvelle catégorie</label><input id="cName" placeholder="Informatique"></div>
        <div class="field" style="flex:0;"><label>&nbsp;</label><button class="ghost" onclick="addCategory()">Créer</button></div>
      </div>
      <div id="msg" class="msg"></div>
    </div>
  </div>
</main>

<script>
const AUTH="http://localhost:5124", CLIENT_ID="postman", REDIRECT="http://localhost:5050/app", SCOPE="openid email profile shop_api";
const tok=()=>sessionStorage.getItem("access_token");

function b64url(bytes){return btoa(String.fromCharCode(...bytes)).replace(/\+/g,'-').replace(/\//g,'_').replace(/=+$/,'');}
function randStr(n){const a=new Uint8Array(n);crypto.getRandomValues(a);return b64url(a).slice(0,n);}
async function sha256b64(s){const b=await crypto.subtle.digest("SHA-256",new TextEncoder().encode(s));return b64url(new Uint8Array(b));}

async function login(){
  const v=randStr(64);sessionStorage.setItem("pkce_v",v);
  const ch=await sha256b64(v);const st=randStr(16);sessionStorage.setItem("st",st);
  location=`${AUTH}/connect/authorize?response_type=code&client_id=${CLIENT_ID}&redirect_uri=${encodeURIComponent(REDIRECT)}&scope=${encodeURIComponent(SCOPE)}&state=${st}&code_challenge=${ch}&code_challenge_method=S256`;
}
function logout(){sessionStorage.removeItem("access_token");location=REDIRECT;}

async function handleRedirect(){
  const p=new URLSearchParams(location.search);const code=p.get("code");
  if(!code)return;
  if(p.get("state")!==sessionStorage.getItem("st")){history.replaceState({},"",REDIRECT);return;}
  const body=new URLSearchParams({grant_type:"authorization_code",code,redirect_uri:REDIRECT,client_id:CLIENT_ID,code_verifier:sessionStorage.getItem("pkce_v")});
  const r=await fetch(`${AUTH}/connect/token`,{method:"POST",headers:{"Content-Type":"application/x-www-form-urlencoded"},body});
  const d=await r.json();
  if(d.access_token)sessionStorage.setItem("access_token",d.access_token);
  history.replaceState({},"",REDIRECT);
}

async function api(path,opts={}){
  const h=opts.headers||{};if(tok())h["Authorization"]="Bearer "+tok();
  return fetch(path,{...opts,headers:h});
}
function show(m,ok){const e=document.getElementById("msg");e.textContent=m;e.className="msg "+(ok?"ok":"ko");}

async function renderAuth(){
  const a=document.getElementById("authArea");
  const form=document.getElementById("addForm"),lock=document.getElementById("addLocked");
  if(!tok()){a.innerHTML=`<button onclick="login()">Se connecter</button>`;form.style.display="none";lock.style.display="block";return;}
  try{
    const r=await api("/api/me");
    if(r.status===401){logout();return;}
    const claims=await r.json();
    const email=(claims.find(c=>c.type==="email")||{}).value||"connecté";
    a.innerHTML=`<span class="badge on">● ${email}</span><button class="ghost" onclick="logout()">Déconnexion</button>`;
    form.style.display="block";lock.style.display="none";
  }catch(e){a.innerHTML=`<button onclick="login()">Se connecter</button>`;}
}

async function loadProducts(){
  const r=await fetch("/api/products");const list=await r.json();
  const b=document.getElementById("productsBody");
  b.innerHTML=list.length?list.map(p=>`<tr><td>${p.id}</td><td>${p.name}</td><td>${p.price} €</td><td>${p.category?p.category.name:p.categoryId}</td></tr>`).join(""):`<tr><td colspan="4" class="locked">Aucun produit.</td></tr>`;
}
async function loadCategories(){
  const r=await fetch("/api/categories");const list=await r.json();
  document.getElementById("pCat").innerHTML=list.length?list.map(c=>`<option value="${c.id}">${c.name}</option>`).join(""):`<option value="">(crée une catégorie)</option>`;
}
async function addProduct(){
  const name=document.getElementById("pName").value.trim();
  const price=parseFloat(document.getElementById("pPrice").value);
  const categoryId=parseInt(document.getElementById("pCat").value);
  if(!name||isNaN(price)||isNaN(categoryId)){show("Renseigne nom, prix et catégorie.",false);return;}
  const r=await api("/api/products",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({name,price,categoryId})});
  if(r.status===401){show("Non autorisé — reconnecte-toi.",false);return;}
  if(r.ok){show("Produit ajouté ✓",true);document.getElementById("pName").value="";document.getElementById("pPrice").value="";loadProducts();}
  else show("Erreur ("+r.status+")",false);
}
async function addCategory(){
  const name=document.getElementById("cName").value.trim();
  if(!name){show("Nom de catégorie requis.",false);return;}
  const r=await api("/api/categories",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({name})});
  if(r.status===401){show("Non autorisé — reconnecte-toi.",false);return;}
  if(r.ok){show("Catégorie créée ✓",true);document.getElementById("cName").value="";loadCategories();}
  else show("Erreur ("+r.status+")",false);
}

(async function init(){
  await handleRedirect();
  await renderAuth();
  await loadProducts();
  await loadCategories();
})();
</script>
</body>
</html>
""";
}
