const $ = id => document.getElementById(id);
const api = async (url, options={}) => {
  const response = await fetch(url, {headers:{'Content-Type':'application/json'}, ...options});
  if (!response.ok) { let msg = await response.text(); throw new Error(msg || `Error HTTP ${response.status}`); }
  return response.status === 204 ? null : response.json();
};
const show = (msg, error=false) => { $('message').textContent=msg; $('message').className=error?'err':'ok'; };
const esc = value => String(value ?? '').replace(/[&<>"']/g, c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
let products=[], categories=[], warehouses=[];
async function loadAll(){
  [products,categories,warehouses] = await Promise.all([api('/api/products'),api('/api/categories'),api('/api/warehouses')]);
  const inventory=await api('/api/inventory'), movements=await api('/api/movements');
  $('totalProducts').textContent=products.length;
  $('totalStock').textContent=inventory.reduce((s,x)=>s+Number(x.quantity),0).toLocaleString('es-EC');
  $('totalMovements').textContent=movements.length;
  $('productsBody').innerHTML=products.map(p=>`<tr><td>${esc(p.sku)}</td><td>${esc(p.name)}</td><td>${esc(p.category?.name||'-')}</td><td>${Number(p.unitCost).toFixed(2)}</td></tr>`).join('');
  $('inventoryBody').innerHTML=inventory.map(x=>`<tr><td>${esc(x.sku)}</td><td>${esc(x.product)}</td><td>${esc(x.warehouse)}</td><td>${Number(x.quantity).toLocaleString('es-EC')}</td></tr>`).join('');
  $('categoryId').innerHTML='<option value="">Sin categoría</option>'+categories.map(c=>`<option value="${c.id}">${esc(c.name)}</option>`).join('');
  $('moveProduct').innerHTML=products.map(p=>`<option value="${p.id}">${esc(p.sku)} - ${esc(p.name)}</option>`).join('');
  $('moveWarehouse').innerHTML=warehouses.map(w=>`<option value="${w.id}">${esc(w.name)}</option>`).join('');
  $('categoriesList').innerHTML=categories.map(c=>`<li>${esc(c.name)}</li>`).join('');
  $('warehousesList').innerHTML=warehouses.map(w=>`<li>${esc(w.name)} ${w.location?'— '+esc(w.location):''}</li>`).join('');
  $('movementsBody').innerHTML=movements.map(m=>`<tr><td>${new Date(m.createdAt).toLocaleString('es-EC')}</td><td>${m.type==='IN'?'Entrada':'Salida'}</td><td>${esc(m.sku)} - ${esc(m.product)}</td><td>${esc(m.warehouse)}</td><td>${Number(m.quantity).toLocaleString('es-EC')}</td><td>${esc(m.reference||'-')}</td></tr>`).join('');
}
document.querySelectorAll('.tab').forEach(b=>b.addEventListener('click',()=>{
  document.querySelectorAll('.tab').forEach(x=>x.classList.remove('active')); b.classList.add('active');
  document.querySelectorAll('.panel').forEach(x=>x.classList.add('hidden')); $(b.dataset.tab).classList.remove('hidden');
}));
$('productForm').addEventListener('submit',async e=>{e.preventDefault();try{
 await api('/api/products',{method:'POST',body:JSON.stringify({sku:$('sku').value,name:$('productName').value,description:$('description').value,unitCost:Number($('unitCost').value),categoryId:$('categoryId').value?Number($('categoryId').value):null})});
 e.target.reset();$('unitCost').value='0';await loadAll();show('Producto guardado correctamente.');
}catch(err){show(err.message,true);}});
$('categoryForm').addEventListener('submit',async e=>{e.preventDefault();try{await api('/api/categories',{method:'POST',body:JSON.stringify({name:$('categoryName').value})});e.target.reset();await loadAll();show('Categoría agregada.');}catch(err){show(err.message,true);}});
$('warehouseForm').addEventListener('submit',async e=>{e.preventDefault();try{await api('/api/warehouses',{method:'POST',body:JSON.stringify({name:$('warehouseName').value,location:$('warehouseLocation').value})});e.target.reset();await loadAll();show('Bodega agregada.');}catch(err){show(err.message,true);}});
$('movementForm').addEventListener('submit',async e=>{e.preventDefault();try{await api('/api/movements',{method:'POST',body:JSON.stringify({productId:Number($('moveProduct').value),warehouseId:Number($('moveWarehouse').value),type:$('moveType').value,quantity:Number($('moveQuantity').value),reference:$('moveReference').value})});e.target.reset();await loadAll();show('Movimiento registrado correctamente.');}catch(err){show(err.message,true);}});
loadAll().catch(err=>show('No se pudo cargar el sistema: '+err.message,true));
