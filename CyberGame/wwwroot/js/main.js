const DATA={
cybers:[
{id:1,name:"TTTM CHỢ MƠ",address:"459 Bạch Mai, Trương Định, Hai Bà Trưng, Hà Nội",machines:250,area:"1000 m²",floors:"2 Tầng",phone:"0363 459459",image:"https://statics.oeg.vn/storage/cms/cyber/location/1786935368_image.png"},
{id:2,name:"175 TRẦN QUỐC HOÀN",address:"175 Trần Quốc Hoàn, Cầu Giấy, Hà Nội",machines:98,area:"80 m²",floors:"5 Tầng",phone:"024 665 65025",image:"https://cyber.oeg.vn/images/hero__banner.webp"},
{id:3,name:"318 LẠC LONG QUÂN",address:"318 Lạc Long Quân, Tây Hồ, Hà Nội",machines:92,area:"80 m²",floors:"4 Tầng",phone:"024 665 65025",image:"https://statics.oeg.vn/storage/cms/cyber/location/1786935368_image.png"},
{id:4,name:"178 TÂY SƠN",address:"178 Tây Sơn, Trung Liệt, Đống Đa, Hà Nội",machines:96,area:"65 m²",floors:"6 Tầng",phone:"024 665 65025",image:"https://cyber.oeg.vn/images/hero__banner.webp"},
{id:5,name:"101 CHIẾN THẮNG",address:"101 Chiến Thắng, Khu đô thị Văn Quán, Hà Đông, Hà Nội",machines:124,area:"80 m²",floors:"7 Tầng",phone:"024 665 65025",image:"https://statics.oeg.vn/storage/cms/cyber/location/1786935368_image.png"},
{id:6,name:"2 TRẦN VỸ",address:"2 Trần Vỹ, Mai Dịch, Cầu Giấy, Hà Nội",machines:96,area:"65 m²",floors:"6 Tầng",phone:"024 665 65025",image:"https://cyber.oeg.vn/images/hero__banner.webp"}],
games:[
{name:"VALORANT",type:"FPS",image:"https://images.unsplash.com/photo-1542751371-adc38448a05e?auto=format&fit=crop&w=1000&q=80"},
{name:"COUNTER-STRIKE 2",type:"FPS",image:"https://images.unsplash.com/photo-1542751110-97427bbecf20?auto=format&fit=crop&w=1000&q=80"},
{name:"LEAGUE OF LEGENDS",type:"MOBA",image:"https://images.unsplash.com/photo-1593305841991-05c297ba4575?auto=format&fit=crop&w=1000&q=80"},
{name:"PUBG",type:"BATTLE ROYALE",image:"https://images.unsplash.com/photo-1560253023-3ec5d502959f?auto=format&fit=crop&w=1000&q=80"},
{name:"DOTA 2",type:"MOBA",image:"https://images.unsplash.com/photo-1548686304-89d188a80029?auto=format&fit=crop&w=1000&q=80"},
{name:"FC ONLINE",type:"SPORT",image:"https://images.unsplash.com/photo-1526232761682-d26e03ac148e?auto=format&fit=crop&w=1000&q=80"}
],
news:[
{date:"13.09.2026",title:"Cập nhật những trải nghiệm gaming mới nhất",desc:"Khám phá không gian và những dịch vụ mới dành cho cộng đồng game thủ.",image:"https://statics.oeg.vn/storage/cms/cyber/location/1786935368_image.png"},
{date:"10.09.2026",title:"Cyber Game - điểm hẹn của cộng đồng",desc:"Không gian gaming hiện đại, kết nối game thủ và những trận đấu đáng nhớ.",image:"https://cyber.oeg.vn/images/hero__banner.webp"},
{date:"05.09.2026",title:"Sự kiện gaming đặc biệt trong tháng",desc:"Theo dõi tin tức để không bỏ lỡ các chương trình và giải đấu.",image:"https://statics.oeg.vn/storage/cms/cyber/location/1786935368_image.png"},
{date:"01.09.2026",title:"Cộng đồng game thủ và những trận đấu đáng nhớ",desc:"Những hoạt động mới nhất đang diễn ra trong hệ sinh thái.",image:"https://cyber.oeg.vn/images/hero__banner.webp"}
]};

document.addEventListener("DOMContentLoaded",()=>{
  setupHeader();setupReveal();setupCounters();setupModal();setupTop();
  if(document.querySelector("#cyberList")) renderHome();
  if(document.querySelector("#cyberPage")) renderCyberPage();
  if(document.querySelector("#cyberDetail")) renderCyberDetail();
  if(document.querySelector("#gamePage")) renderGamesPage();
  if(document.querySelector("#newsPage")) renderNewsPage();
  if(document.querySelector("#menuPage")) renderMenuPage();
  if(document.querySelector("#articlePage")) renderArticle();
});

function setupHeader(){
 const h=document.querySelector("header"),nav=document.querySelector("nav"),b=document.querySelector(".hamb");
 window.addEventListener("scroll",()=>h?.classList.toggle("scrolled",scrollY>30));
 b?.addEventListener("click",()=>{nav.classList.toggle("open");b.classList.toggle("open")});
 nav?.querySelectorAll("a").forEach(a=>a.addEventListener("click",()=>nav.classList.remove("open")));
}
function setupReveal(){
 const obs=new IntersectionObserver(es=>es.forEach(e=>{if(e.isIntersecting){e.target.classList.add("visible");obs.unobserve(e.target)}}),{threshold:.08});
 document.querySelectorAll(".reveal").forEach(x=>obs.observe(x));
 window.reveal=()=>document.querySelectorAll(".reveal:not(.visible)").forEach(x=>obs.observe(x));
}
function setupCounters(){
 const obs=new IntersectionObserver(es=>es.forEach(e=>{if(e.isIntersecting){let el=e.target,t=+el.dataset.count,s=performance.now();function f(n){let p=Math.min((n-s)/1000,1),v=Math.floor(t*(1-Math.pow(1-p,3)));el.textContent=v.toLocaleString("vi-VN");if(p<1)requestAnimationFrame(f)}requestAnimationFrame(f);obs.unobserve(el)}}),{threshold:.3});
 document.querySelectorAll("[data-count]").forEach(x=>obs.observe(x));
}
function setupModal(){
 const modal=document.querySelector("#modal"),btn=document.querySelector("#accountBtn");
 btn?.addEventListener("click",()=>modal?.classList.add("open"));
 modal?.querySelectorAll("[data-close]").forEach(x=>x.addEventListener("click",()=>modal.classList.remove("open")));
 document.addEventListener("keydown",e=>e.key==="Escape"&&modal?.classList.remove("open"));
}
function setupTop(){
 const b=document.querySelector(".top");if(!b)return;
 addEventListener("scroll",()=>b.classList.toggle("show",scrollY>600));b.onclick=()=>scrollTo({top:0,behavior:"smooth"});
}
function cyberCard(c,i){
 return `<article class="cyber reveal"><div class="photo" style="background-image:url('${c.image}')"></div><div class="cyber-body"><span class="tag">CYBER ${String(i+1).padStart(2,"0")}</span><h3>${c.name}</h3><p class="address">${c.address}</p><div class="meta"><div><strong>${c.machines}</strong><small>SỨC CHỨA</small></div><div><strong>${c.area}</strong><small>DIỆN TÍCH</small></div><div><strong>${c.floors}</strong><small>SỐ TẦNG</small></div></div><div class="mini"><a href="/Home/CyberDetail?id=${c.id}">XEM THÔNG TIN</a><a target="_blank" href="https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(c.address)}">XEM BẢN ĐỒ</a><a href="tel:${c.phone.replaceAll(" ","")}">GỌI HOTLINE</a></div></div></article>`;
}
function renderHome(){
 const list=document.querySelector("#cyberList");if(list)list.innerHTML=DATA.cybers.map(cyberCard).join("");
 const gg=document.querySelector("#gameGrid");if(gg)gg.innerHTML=DATA.games.slice(0,4).map((g,i)=>`<a class="game reveal" href="/Home/Games"><img src="${g.image}" alt="${g.name}" loading="lazy"><div class="game-info"><small>${g.type}</small><h3>${g.name}</h3></div></a>`).join("");
 const ng=document.querySelector("#newsGrid");if(ng)ng.innerHTML=DATA.news.slice(0,3).map((n,i)=>`<article class="news-card reveal"><a class="news-img" href="/Home/NewsDetail?id=${i+1}" style="background-image:url('${n.image}')"></a><div class="news-body"><time>${n.date}</time><h3>${n.title}</h3><p>${n.desc}</p></div></article>`).join("");
 reveal();
}
function renderCyberPage(){
 const list=document.querySelector("#allCybers");list.innerHTML=DATA.cybers.map(cyberCard).join("");reveal();
}
function renderCyberDetail(){
 let id=+(new URLSearchParams(location.search).get("id")||1),c=DATA.cybers.find(x=>x.id===id)||DATA.cybers[0];
 document.title=`${c.name} | Cyber Game`;
 document.querySelector("#detailName").textContent=c.name;
 document.querySelector("#detailAddress").textContent=c.address;
 document.querySelector("#detailPhone").textContent=c.phone;
 document.querySelector("#detailMap").href=`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(c.address)}`;
 document.querySelector("#detailHeroImage").style.backgroundImage=`url('${c.image}')`;
 document.querySelector("#specs").innerHTML=[["SỨC CHỨA",c.machines+" Máy"],["DIỆN TÍCH",c.area],["SỐ TẦNG",c.floors],["HOẠT ĐỘNG","24/7"],["PARKING","Có"],["WIFI","High Speed"]].map(x=>`<div class="spec"><small>${x[0]}</small><strong>${x[1]}</strong></div>`).join("");
}
function renderGamesPage(){
 const g=document.querySelector("#allGames");g.innerHTML=DATA.games.map(x=>`<a class="game reveal" href="#"><img src="${x.image}" alt="${x.name}"><div class="game-info"><small>${x.type}</small><h3>${x.name}</h3></div></a>`).join("");reveal();
}
function renderNewsPage(){
 const g=document.querySelector("#allNews");g.innerHTML=DATA.news.map((n,i)=>`<article class="news-card reveal"><a class="news-img" href="/Home/NewsDetail?id=${i+1}" style="background-image:url('${n.image}')"></a><div class="news-body"><time>${n.date}</time><h3>${n.title}</h3><p>${n.desc}</p></div></article>`).join("");reveal();
}
function renderArticle(){
 let id=+(new URLSearchParams(location.search).get("id")||1),n=DATA.news[id-1]||DATA.news[0];
 document.title=n.title+" | Cyber Game";
 document.querySelector("#articleTitle").textContent=n.title;
 document.querySelector("#articleDate").textContent=n.date;
 document.querySelector("#articleImage").src=n.image;
}

DATA.zones = [
  { id: 'standard', name: 'Standard Zone', price: 10000, prefix: 'S', max: 40 },
  { id: 'vip', name: 'VIP Zone', price: 15000, prefix: 'V', max: 20 },
  { id: 'pro', name: 'Pro Stage', price: 20000, prefix: 'P', max: 10 },
  { id: 'stream', name: 'Stream Room', price: 35000, prefix: 'R', max: 5 }
];

DATA.menu = [
  { id: 'f1', name: 'Mì xào bò', price: 35000, category: 'food', image: 'https://images.unsplash.com/photo-1612929633738-8fe44f7ec841?auto=format&fit=crop&w=400&q=80' },
  { id: 'f2', name: 'Cơm rang dưa bò', price: 45000, category: 'food', image: 'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=400&q=80' },
  { id: 'f4', name: 'Bánh mì pate trứng', price: 25000, category: 'food', image: 'https://images.unsplash.com/photo-1606850239638-b78f8b8e0508?auto=format&fit=crop&w=400&q=80' },
  { id: 'd1', name: 'Sting Dâu', price: 15000, category: 'drink', image: 'https://images.unsplash.com/photo-1622483767028-3f66f32aef97?auto=format&fit=crop&w=400&q=80' },
  { id: 'd2', name: 'Bò húc (Redbull)', price: 20000, category: 'drink', image: 'https://images.unsplash.com/photo-1622483767028-3f66f32aef97?auto=format&fit=crop&w=400&q=80' }, 
  { id: 'd3', name: 'Coca Cola', price: 15000, category: 'drink', image: 'https://images.unsplash.com/photo-1622483767028-3f66f32aef97?auto=format&fit=crop&w=400&q=80' },
  { id: 'd4', name: 'Trà đào cam sả', price: 30000, category: 'drink', image: 'https://images.unsplash.com/photo-1556679343-c7306c1976bc?auto=format&fit=crop&w=400&q=80' },
  { id: 's1', name: 'Khoai tây chiên', price: 25000, category: 'snack', image: 'https://images.unsplash.com/photo-1576107232684-1279f3908594?auto=format&fit=crop&w=400&q=80' },
  { id: 's2', name: 'Xúc xích Đức', price: 15000, category: 'snack', image: 'https://images.unsplash.com/photo-1599598425947-33002620ea1f?auto=format&fit=crop&w=400&q=80' },
  { id: 's3', name: 'Khô gà lá chanh', price: 30000, category: 'snack', image: 'https://images.unsplash.com/photo-1621996316521-8789db4c8ff9?auto=format&fit=crop&w=400&q=80' },
  { id: 's4', name: 'Đậu phộng rang tỏi ớt', price: 15000, category: 'snack', image: 'https://images.unsplash.com/photo-1571556948574-d023f0343a41?auto=format&fit=crop&w=400&q=80' },
  { id: 'u1', name: 'Bọc tai nghe (1 lần)', price: 5000, category: 'utility', image: 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=400&q=80' },
  { id: 'u2', name: 'Áo mưa dùng 1 lần', price: 10000, category: 'utility', image: 'https://images.unsplash.com/photo-1515694346937-94d85e41e6f0?auto=format&fit=crop&w=400&q=80' },
  { id: 'u3', name: 'Gối chữ U kê cổ', price: 20000, category: 'utility', image: 'https://images.unsplash.com/photo-1584100936595-c0654b35a113?auto=format&fit=crop&w=400&q=80' },
  { id: 'u4', name: 'Chăn đắp mỏng', price: 25000, category: 'utility', image: 'https://images.unsplash.com/photo-1580252541459-7b3d328325db?auto=format&fit=crop&w=400&q=80' }
];

let chosenSeats = [];
let chosenFood = {};

document.addEventListener("DOMContentLoaded", () => {
  setupChatbot();
  if(document.querySelector("#bookingPage")) renderBookingPage();
});

  function renderSeatMap(zoneId, zoneName) {
    const wrap = document.querySelector("#seatMapWrap");
    if(!wrap) return;
    
    if(!zoneId) {
      let skeletonHTML = '';
      for(let i=1; i<=40; i++) {
        skeletonHTML += `<div class="seat skeleton"></div>`;
      }
      
      wrap.innerHTML = `
        <button class="close" data-close-seat title="Đóng">×</button>
        <div class="seat-map-header">
          <div class="seat-map-title">SƠ ĐỒ PHÒNG MÁY</div>
          <div class="seat-legend">
            <div class="legend-item"><div class="legend-color legend-available"></div> Trống</div>
            <div class="legend-item"><div class="legend-color legend-selected"></div> Đang chọn</div>
            <div class="legend-item"><div class="legend-color legend-inuse"></div> Đã có người</div>
          </div>
        </div>
        <div class="seat-grid">
          ${skeletonHTML}
        </div>
      `;
      chosenSeats = [];
      return;
    }
  
  const zone = DATA.zones.find(z => z.id === zoneId);
  const totalSeats = zone.max;
  
  let seatsHTML = '';
  for(let i=1; i<=totalSeats; i++) {
    let status = 'available';
    let seatLabel = zone.prefix + i.toString().padStart(2, '0');
    
    if(chosenSeats.includes(seatLabel)) {
      status += ' selected';
    }
    
    seatsHTML += `<div class="seat ${status}" data-id="${seatLabel}">${seatLabel}</div>`;
  }
  
  wrap.innerHTML = `
    <button class="close" data-close-seat title="Đóng">×</button>
    <div class="seat-map-header">
      <div class="seat-map-title">Sơ đồ máy - ${zoneName}</div>
      <div class="seat-legend">
        <div class="legend-item"><div class="legend-color legend-available"></div> Rảnh</div>
        <div class="legend-item"><div class="legend-color legend-selected"></div> Đang chọn</div>
        <div class="legend-item"><div class="legend-color legend-inuse"></div> Có người</div>
      </div>
    </div>
    <div class="seat-grid">
      ${seatsHTML}
    </div>
    <div class="seat-status-bar" id="seatStatusBar"></div>
    <div style="margin-top: 18px; text-align: center;">
      <button type="button" class="btn blue" data-close-seat style="padding: 12px 32px; border-radius: 8px; font-size: 13px; font-weight: 700; cursor: pointer;">Xác nhận chọn máy</button>
    </div>
  `;

  function updateSeatStatusBar(msg = '') {
    const statusEl = wrap.querySelector("#seatStatusBar");
    if(!statusEl) return;
    const peopleCount = parseInt(document.querySelector("#bPeople")?.value) || 1;
    const chosenCount = chosenSeats.length;
    
    let infoText = '';
    if(chosenCount === 0) {
      infoText = `<span style="color:#a1aab5">Chưa chọn máy nào (<strong>${chosenCount}/${peopleCount}</strong> máy)</span>`;
    } else if(chosenCount < peopleCount) {
      infoText = `<span>Đang chọn: <strong style="color:var(--blue2)">${chosenSeats.join(', ')}</strong> (${chosenCount}/${peopleCount} máy - Cần thêm ${peopleCount - chosenCount})</span>`;
    } else {
      infoText = `<span>Đang chọn: <strong style="color:var(--blue2)">${chosenSeats.join(', ')}</strong> (${chosenCount}/${peopleCount} máy - Đã đủ)</span>`;
    }
    
    let toastHTML = msg 
      ? `<span class="seat-swap-toast">${msg}</span>` 
      : `<span style="font-size:11px;color:#6b7280">💡 Bấm máy đang chọn để hủy, hoặc chọn máy mới để đổi</span>`;
    
    statusEl.innerHTML = `
      <div>${infoText}</div>
      <div>${toastHTML}</div>
    `;
  }

  updateSeatStatusBar();
  
  wrap.querySelectorAll('.seat.available, .seat.selected').forEach(el => {
    el.addEventListener('click', function() {
      const peopleCount = parseInt(document.querySelector("#bPeople")?.value) || 1;
      const seatId = this.dataset.id;
      let toastMsg = '';
      
      if(this.classList.contains('selected')) {
        // Hủy chọn máy này nếu đã chọn
        this.classList.remove('selected');
        chosenSeats = chosenSeats.filter(id => id !== seatId);
        toastMsg = `Đã bỏ chọn máy <strong>${seatId}</strong>`;
      } else {
        // Chọn máy mới
        if(chosenSeats.length >= peopleCount) {
          // Khi đã đủ số lượng máy -> Tự động đổi máy (FIFO: Bỏ máy cũ nhất, thêm máy mới)
          const oldSeatId = chosenSeats.shift();
          const oldEl = wrap.querySelector(`.seat[data-id="${oldSeatId}"]`);
          if(oldEl) {
            oldEl.classList.remove('selected');
          }
          
          this.classList.add('selected');
          chosenSeats.push(seatId);
          
          if(peopleCount === 1) {
            toastMsg = `🔄 Đã chuyển sang máy <strong>${seatId}</strong>`;
          } else {
            toastMsg = `🔄 Đã đổi máy <strong>${oldSeatId}</strong> ➔ <strong>${seatId}</strong>`;
          }
        } else {
          // Chưa đủ số lượng -> Thêm máy mới
          this.classList.add('selected');
          chosenSeats.push(seatId);
          toastMsg = `✔ Đã chọn máy <strong>${seatId}</strong>`;
        }
      }
      
      updateSeatStatusBar(toastMsg);
      document.querySelector("#bDuration").dispatchEvent(new Event('input'));
    });
  });
}

window.updateFoodQty = function(input) {
  const id = input.dataset.id;
  const qty = parseInt(input.value) || 0;
  if(qty > 0) {
    chosenFood[id] = { qty, price: parseInt(input.dataset.price), name: DATA.menu.find(x=>x.id===id).name };
  } else {
    delete chosenFood[id];
  }
  document.querySelector("#bDuration").dispatchEvent(new Event('input'));
};

const DRAFT_STORAGE_KEY = 'cybergame_booking_draft';

function renderBookingPage() {
  const nameInput = document.querySelector("#bName");
  const phoneInput = document.querySelector("#bPhone");
  const peopleInput = document.querySelector("#bPeople");
  const zoneSelect = document.querySelector("#bZone");
  const dateTimeInput = document.querySelector("#bDateTime");
  const durationInput = document.querySelector("#bDuration");
  const paymentSelect = document.querySelector("#bPayment");
  const summaryBox = document.querySelector("#bSummary");
  const form = document.querySelector("#bookingForm");
  
  const menuWrap = document.querySelector("#bookingMenuWrap");
  const menuList = document.querySelector("#bookingMenuList");
  const seatModal = document.querySelector("#seatModal");
  const foodModal = document.querySelector("#foodModal");
  const btnOpenFood = document.querySelector("#btnOpenFoodModal");
  
  if(!zoneSelect) return;

  let lastZone = "";
  let isSelectingZone = false;

  // Render Food options
  if(menuList) {
    menuList.innerHTML = DATA.menu.map(m => `
      <div style="display:flex;justify-content:space-between;align-items:center;background:#232830;padding:10px;border-radius:6px">
        <div style="display:flex;align-items:center;gap:10px">
          <img src="${m.image}" style="width:40px;height:40px;border-radius:4px;object-fit:cover">
          <div>
            <div style="font-size:13px;font-weight:600">${m.name}</div>
            <div style="font-size:11px;color:var(--blue)">${m.price.toLocaleString('vi-VN')} đ</div>
          </div>
        </div>
        <div style="display:flex;align-items:center;gap:5px">
          <button type="button" class="btn-qty" onclick="this.nextElementSibling.stepDown(); this.nextElementSibling.dispatchEvent(new Event('input'))" style="width:24px;height:24px;border:none;background:#38404a;color:#fff;border-radius:4px;cursor:pointer">-</button>
          <input type="number" min="0" max="20" value="0" data-id="${m.id}" data-price="${m.price}" data-name="${m.name}" style="width:35px;text-align:center;background:transparent;border:1px solid #38404a;color:#fff;padding:2px;border-radius:4px">
          <button type="button" class="btn-qty" onclick="this.previousElementSibling.stepUp(); this.previousElementSibling.dispatchEvent(new Event('input'))" style="width:24px;height:24px;border:none;background:#38404a;color:#fff;border-radius:4px;cursor:pointer">+</button>
        </div>
      </div>
    `).join('');

    menuList.querySelectorAll('input[type="number"]').forEach(input => {
      input.addEventListener('input', (e) => {
        let val = parseInt(e.target.value) || 0;
        if(val < 0) val = 0;
        if(val > 20) val = 20;
        e.target.value = val;
        
        const fid = e.target.getAttribute('data-id');
        const price = parseInt(e.target.getAttribute('data-price'));
        const name = e.target.getAttribute('data-name');
        
        if(val > 0) {
          chosenFood[fid] = { name: name, price: price, qty: val };
        } else {
          delete chosenFood[fid];
        }
        durationInput.dispatchEvent(new Event('input')); // Trigger summary update & draft save
      });
    });
  }
  
  zoneSelect.innerHTML = `<option value="">-- Chọn khu vực --</option>` + DATA.zones.map(z => `<option value="${z.id}" data-price="${z.price}">${z.name} - ${z.price.toLocaleString('vi-VN')}đ/h</option>`).join("");

  // Modal handlers
  if(seatModal) {
    seatModal.addEventListener("click", (e) => {
      if(e.target.matches("[data-close-seat]") || e.target.closest("[data-close-seat]")) {
        seatModal.classList.remove("open");
      }
    });
  }
  
  if(btnOpenFood && foodModal) {
    btnOpenFood.addEventListener("click", () => foodModal.classList.add("open"));
    document.querySelectorAll("[data-close-food]").forEach(el => {
      el.addEventListener("click", () => foodModal.classList.remove("open"));
    });
  }

  // Zone selection interaction:
  // Khi khách bấm vào chọn khu vực loại máy, nếu đã có khu vực đang chọn thì đặt lại value="" để khi bấm bất kỳ khu vực nào (kể cả cùng khu vực) đều kích hoạt change để mở lại sơ đồ
  const handleZoneInteractionStart = () => {
    if(zoneSelect.value) {
      lastZone = zoneSelect.value;
      isSelectingZone = true;
      zoneSelect.value = "";
    }
  };

  zoneSelect.addEventListener("mousedown", handleZoneInteractionStart);
  zoneSelect.addEventListener("touchstart", handleZoneInteractionStart, { passive: true });

  zoneSelect.addEventListener("change", (e) => {
    isSelectingZone = false;
    const val = e.target.value;
    if(val) {
      // Nếu đổi sang khu vực khác -> reset máy đã chọn. Nếu chọn lại cùng khu vực -> giữ nguyên máy đang chọn
      if(lastZone && val !== lastZone) {
        chosenSeats = [];
      }
      lastZone = val;
      const zoneOpt = zoneSelect.options[zoneSelect.selectedIndex];
      renderSeatMap(val, zoneOpt.text.split(' - ')[0]);
      if(seatModal) seatModal.classList.add("open");
    } else {
      chosenSeats = [];
      lastZone = "";
      renderSeatMap(null, null);
    }
    updateSummary();
    saveBookingDraft();
  });

  zoneSelect.addEventListener("blur", () => {
    if(isSelectingZone && !zoneSelect.value && lastZone) {
      zoneSelect.value = lastZone;
      isSelectingZone = false;
    }
  });

  durationInput.addEventListener("input", () => {
    updateSummary();
    saveBookingDraft();
  });
  
  if(peopleInput) {
    peopleInput.addEventListener("input", () => {
      const peopleCount = parseInt(peopleInput.value) || 1;
      if(chosenSeats.length > peopleCount) {
        chosenSeats = chosenSeats.slice(0, peopleCount);
        if(zoneSelect.value) {
          const zoneOpt = zoneSelect.options[zoneSelect.selectedIndex];
          renderSeatMap(zoneSelect.value, zoneOpt.text.split(' - ')[0]);
        }
      }
      updateSummary();
      saveBookingDraft();
    });
  }

  if(nameInput) nameInput.addEventListener("input", saveBookingDraft);
  if(phoneInput) phoneInput.addEventListener("input", saveBookingDraft);
  if(paymentSelect) paymentSelect.addEventListener("change", () => {
    updateSummary();
    saveBookingDraft();
  });
  if(dateTimeInput) {
    dateTimeInput.addEventListener("input", saveBookingDraft);
    dateTimeInput.addEventListener("change", saveBookingDraft);
  }

  // Quản lý lưu trữ tạm thời (draft) khi reload trang
  function saveBookingDraft() {
    if(!form) return;
    const draft = {
      name: nameInput ? nameInput.value : '',
      phone: phoneInput ? phoneInput.value : '',
      people: peopleInput ? peopleInput.value : '1',
      zone: zoneSelect ? zoneSelect.value : '',
      seats: chosenSeats || [],
      dateTime: dateTimeInput ? dateTimeInput.value : '',
      duration: durationInput ? durationInput.value : '2',
      payment: paymentSelect ? paymentSelect.value : 'counter',
      food: chosenFood || {}
    };
    try {
      localStorage.setItem(DRAFT_STORAGE_KEY, JSON.stringify(draft));
    } catch(e) {}
  }
  window.saveBookingDraft = saveBookingDraft;

  function loadBookingDraft() {
    try {
      const raw = localStorage.getItem(DRAFT_STORAGE_KEY);
      if(!raw) return false;
      const draft = JSON.parse(raw);
      if(!draft || typeof draft !== 'object') return false;

      if(draft.name !== undefined && nameInput) nameInput.value = draft.name;
      if(draft.phone !== undefined && phoneInput) phoneInput.value = draft.phone;
      if(draft.people !== undefined && peopleInput) peopleInput.value = draft.people;
      if(draft.duration !== undefined && durationInput) durationInput.value = draft.duration;
      if(draft.payment !== undefined && paymentSelect) paymentSelect.value = draft.payment;
      if(draft.dateTime !== undefined && dateTimeInput) {
        dateTimeInput.value = draft.dateTime;
        if(dateTimeInput._flatpickr) {
          dateTimeInput._flatpickr.setDate(draft.dateTime, false);
        }
      }

      if(draft.food && typeof draft.food === 'object') {
        chosenFood = draft.food;
        if(menuList) {
          menuList.querySelectorAll('input[type="number"]').forEach(input => {
            const fid = input.getAttribute('data-id');
            input.value = (chosenFood[fid] && chosenFood[fid].qty) ? chosenFood[fid].qty : 0;
          });
        }
      }

      if(draft.zone && zoneSelect) {
        zoneSelect.value = draft.zone;
        lastZone = draft.zone;
        if(Array.isArray(draft.seats)) {
          chosenSeats = draft.seats;
        }
        const zoneOpt = zoneSelect.options[zoneSelect.selectedIndex];
        if(zoneOpt && zoneOpt.value) {
          renderSeatMap(draft.zone, zoneOpt.text.split(' - ')[0]);
        } else {
          renderSeatMap(null, null);
        }
      } else {
        renderSeatMap(null, null);
      }

      updateSummary();
      return true;
    } catch(e) {
      console.warn("Lỗi phục hồi bản lưu đặt chỗ:", e);
      return false;
    }
  }

  function clearBookingDraft() {
    try {
      localStorage.removeItem(DRAFT_STORAGE_KEY);
    } catch(e) {}
    chosenSeats = [];
    chosenFood = {};
    lastZone = "";
    if(form) form.reset();
    if(menuList) {
      menuList.querySelectorAll('input[type="number"]').forEach(input => input.value = 0);
    }
    renderSeatMap(null, null);
    updateSummary();
  }

  // Khôi phục nếu có bản lưu trước đó
  const hasDraft = loadBookingDraft();
  if(!hasDraft) {
    renderSeatMap(null, null);
    updateSummary();
  }

  // Ngăn chặn vô tình nhấn Enter trong ô nhập liệu gửi form sớm
  form.addEventListener("keydown", (e) => {
    if(e.key === "Enter" && e.target.tagName === "INPUT") {
      e.preventDefault();
    }
  });

  function updateSummary() {
    const zoneOpt = zoneSelect.options[zoneSelect.selectedIndex];
    const duration = parseInt(durationInput.value) || 0;
    const peopleCount = parseInt(peopleInput?.value) || 1;
    
    const price = (zoneOpt && zoneOpt.value) ? parseInt(zoneOpt.dataset.price) : 0;
    
    let foodTotal = 0;
    let foodInfo = '';
    if(Object.keys(chosenFood).length > 0) {
      const items = Object.values(chosenFood).map(f => `${f.name} (x${f.qty})`).join(', ');
      foodTotal = Object.values(chosenFood).reduce((sum, f) => sum + (f.price * f.qty), 0);
      foodInfo = `<div class="summary-item"><span>Đồ ăn/uống:</span> <span style="text-align:right;max-width:200px">${items}</span></div>`;
    }
    
    if(!price && foodTotal === 0) {
      summaryBox.innerHTML = `<h3 style="color: var(--blue2); font-size: 18px; margin-bottom: 15px; font-weight: 700; text-transform: uppercase;">TỔNG QUAN ĐẶT CHỖ</h3><p style="color:var(--text-gray); font-size:14px">Vui lòng điền đủ thông tin để xem tạm tính.</p>`;
      return;
    }
    
    const total = (price * duration * peopleCount) + foodTotal;
    
    let html = `<h3 style="color: var(--blue2); font-size: 18px; margin-bottom: 15px; font-weight: 700; text-transform: uppercase;">TỔNG QUAN ĐẶT CHỖ</h3>`;
    
    if(price > 0) {
      let seatInfo = chosenSeats.length > 0 
        ? `<span style="color:var(--blue2);font-weight:700">${chosenSeats.join(', ')}</span>` 
        : `<span style="color:#bf616a">Chưa chọn chỗ (${chosenSeats.length}/${peopleCount}) máy</span>`;
        
      if(chosenSeats.length > 0 && chosenSeats.length < peopleCount) {
          seatInfo = `<span style="color:#bf616a">${chosenSeats.join(', ')} (Còn thiếu ${peopleCount - chosenSeats.length} máy)</span>`;
      }
      
      html += `
        <div class="summary-item" id="summaryZoneRow" style="cursor:pointer;" title="Nhấp để chọn lại hoặc đổi máy"><span>Khu vực:</span> <span>${zoneOpt.text.split(' - ')[0]}</span></div>
        <div class="summary-item" id="summarySeatRow" style="cursor:pointer;" title="Nhấp để chọn lại hoặc đổi máy"><span>Vị trí máy:</span> <span>${seatInfo}</span></div>
        <div class="summary-item"><span>Số lượng:</span> <span>${peopleCount} người x ${duration} giờ</span></div>
      `;
    }
    
    html += foodInfo;
    html += `<div class="summary-total"><span>Tổng tiền:</span> <span>${total.toLocaleString('vi-VN')} VNĐ</span></div>`;
    
    summaryBox.innerHTML = html;

    // Bấm vào khu vực hoặc vị trí máy trong bảng tóm tắt cũng có thể mở lại sơ đồ chọn máy
    const sZone = summaryBox.querySelector("#summaryZoneRow");
    const sSeat = summaryBox.querySelector("#summarySeatRow");
    const openSeatModalHandler = () => {
      if(zoneSelect.value) {
        const opt = zoneSelect.options[zoneSelect.selectedIndex];
        renderSeatMap(zoneSelect.value, opt.text.split(' - ')[0]);
        if(seatModal) seatModal.classList.add("open");
      }
    };
    if(sZone) sZone.onclick = openSeatModalHandler;
    if(sSeat) sSeat.onclick = openSeatModalHandler;
  }
  
  form.addEventListener("submit", (e) => {
    e.preventDefault();
    const peopleCount = parseInt(peopleInput?.value) || 1;
    if(chosenSeats.length !== peopleCount) {
      alert(`Vui lòng chọn đủ ${peopleCount} máy rảnh trên sơ đồ tương ứng với số người!`);
      return;
    }
    
    function showSuccess() {
      // Xác nhận thanh toán/đặt máy thành công -> Xóa dữ liệu tạm thời và reset form
      clearBookingDraft();
      const modal = document.querySelector("#bookingSuccessModal");
      if(modal) {
        modal.classList.add("open");
        modal.querySelectorAll("[data-close-success]").forEach(x => {
          x.onclick = () => {
            modal.classList.remove("open");
          };
        });
      } else {
        alert("Đặt máy thành công! Cảm ơn bạn.");
      }
    }

    const paymentMethod = document.querySelector("#bPayment").value;
    if (paymentMethod === 'transfer') {
      const qrModal = document.querySelector("#qrModal");
      if(qrModal) {
        const totalText = summaryBox.querySelector(".summary-total span:last-child").textContent;
        qrModal.querySelector("#qrTotalAmount").textContent = totalText;
        qrModal.classList.add("open");
        
        qrModal.querySelector("#btnConfirmPayment").onclick = () => {
          qrModal.classList.remove("open");
          showSuccess();
        };
        qrModal.querySelectorAll("[data-close-qr]").forEach(btn => {
          btn.onclick = () => qrModal.classList.remove("open");
        });
      } else {
        showSuccess();
      }
    } else {
      showSuccess();
    }
  });
}

function setupChatbot() {
  const STORAGE_CHAT_KEY = 'cybergame_chat_conversations_v3';
  const CLIENT_CHAT_SESSION_KEY = 'cybergame_current_client_chat_id';
  let clientChatId = localStorage.getItem(CLIENT_CHAT_SESSION_KEY);
  if (!clientChatId) {
    clientChatId = 'conv_web_' + Date.now().toString().slice(-6);
    localStorage.setItem(CLIENT_CHAT_SESSION_KEY, clientChatId);
  }

  const html = `
    <div class="chatbot-widget" id="chatbotWidget">
      <button class="chat-btn" id="chatBtn" title="Trò chuyện hỗ trợ">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z"></path></svg>
      </button>
      <div class="chat-window">
        <div class="chat-head">
          <h4>Trợ lý CyberGame</h4>
          <button class="chat-close" id="chatClose">×</button>
        </div>
        <div class="chat-body" id="chatBody">
          <div class="msg bot">Chào bạn! Tôi là trợ lý AI của CyberGame. Bạn cần hỗ trợ gì về giá máy, cấu hình hay đặt chỗ? Nếu cần gặp người hỗ trợ, cứ nhắn cho mình nhé!</div>
        </div>
        <form class="chat-foot" id="chatForm">
          <input type="text" class="chat-input" id="chatInput" placeholder="Nhập tin nhắn..." autocomplete="off">
          <button type="submit" class="chat-send">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="22" y1="2" x2="11" y2="13"></line><polygon points="22 2 15 22 11 13 2 9 22 2"></polygon></svg>
          </button>
        </form>
      </div>
    </div>
  `;
  document.body.insertAdjacentHTML('beforeend', html);
  
  const widget = document.getElementById('chatbotWidget');
  const chatBtn = document.getElementById('chatBtn');
  const chatClose = document.getElementById('chatClose');
  const chatForm = document.getElementById('chatForm');
  const chatInput = document.getElementById('chatInput');
  const chatBody = document.getElementById('chatBody');

  const renderedMsgIds = new Set();
  
  chatBtn.addEventListener('click', () => {
    widget.classList.toggle('open');
    chatBtn.style.animation = 'none';
  });
  chatClose.addEventListener('click', () => widget.classList.remove('open'));

  // Load existing chat history from storage if available
  function syncMessagesFromStorage() {
    const raw = localStorage.getItem(STORAGE_CHAT_KEY);
    if (!raw) return;
    try {
      const convs = JSON.parse(raw);
      const conv = convs.find(c => c.id === clientChatId);
      if (!conv || !conv.messages) return;

      conv.messages.forEach(m => {
        if (!renderedMsgIds.has(m.id)) {
          renderedMsgIds.add(m.id);
          if (m.sender === 'admin') {
            const formatted = m.text.replace(/\n/g, '<br>');
            chatBody.insertAdjacentHTML('beforeend', `
              <div class="msg admin">
                <div class="msg-admin-header">👑 Quản Trị Viên</div>
                ${formatted}
              </div>
            `);
            chatBody.scrollTop = chatBody.scrollHeight;
            if (!widget.classList.contains('open')) {
              chatBtn.style.animation = 'pulse-dot 1.2s infinite';
            }
          }
        }
      });
    } catch (e) {}
  }

  // Initial sync
  syncMessagesFromStorage();

  // Storage listener for live incoming admin replies
  window.addEventListener('storage', (e) => {
    if (e.key === STORAGE_CHAT_KEY) {
      syncMessagesFromStorage();
    }
  });

  // Polling check every 1.5 seconds
  setInterval(syncMessagesFromStorage, 1500);
  
  chatForm.addEventListener('submit', (e) => {
    e.preventDefault();
    const txt = chatInput.value.trim();
    if(!txt) return;
    
    // 1. Show user message in chat
    const userMsgId = 'msg_user_' + Date.now();
    renderedMsgIds.add(userMsgId);
    chatBody.insertAdjacentHTML('beforeend', `<div class="msg user">${txt}</div>`);
    chatInput.value = '';
    chatBody.scrollTop = chatBody.scrollHeight;

    const now = new Date();
    const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;

    // 2. Update conversation record in storage
    let raw = localStorage.getItem(STORAGE_CHAT_KEY);
    let convs = [];
    try {
      convs = raw ? JSON.parse(raw) : [];
    } catch(err) {
      convs = [];
    }

    let conv = convs.find(c => c.id === clientChatId);
    if (!conv) {
      conv = {
        id: clientChatId,
        customerName: 'Khách Trực Tuyến (Web)',
        email: '',
        seat: 'Website Khách',
        avatarText: 'KH',
        isOnline: true,
        status: 'bot_active',
        unread: true,
        updatedAt: 'Vừa xong',
        messages: []
      };
      convs.unshift(conv);
    }

    conv.isOnline = true;
    conv.updatedAt = 'Vừa xong';
    if (!conv.messages) conv.messages = [];
    conv.messages.push({
      id: userMsgId,
      sender: 'customer',
      text: txt,
      time: timeStr,
      date: 'Hôm nay'
    });
    
    // 3. Process Bot Logic or Fallback Handover to Admin
    setTimeout(() => {
      const lower = txt.toLowerCase();
      let reply = null;
      let isHandover = false;

      // Rule-based automatic bot knowledge
      if (lower.includes("giá") || lower.includes("bao nhiêu") || lower.includes("tiền") || lower.includes("bảng giá")) {
        reply = "Giá phòng Standard là 10k/h, VIP là 15k/h, Pro Stage là 20k/h và Stream Room là 35k/h bạn nhé!";
      } else if (lower.includes("đặt") || lower.includes("booking") || lower.includes("giữ chỗ")) {
        reply = "Bạn có thể truy cập mục ĐẶT MÁY trên thanh menu hoặc click <a href='/Home/Booking' style='color:#64a9ff;text-decoration:underline'>vào đây</a> để đặt chỗ nhé.";
      } else if (lower.includes("cấu hình") || lower.includes("máy") || lower.includes("gear") || lower.includes("màn hình")) {
        reply = "Hệ thống CyberGame sử dụng Card đồ họa RTX 3060 - 4080, Màn hình 144Hz - 240Hz, cùng chuột và phím cơ Logitech/Razer cao cấp.";
      } else if (lower.includes("chào") || lower.includes("hello") || lower.includes("hi")) {
        reply = "Chào bạn! Mình có thể hỗ trợ gì về dịch vụ máy, nạp tiền hay đồ ăn cho bạn?";
      } else {
        // FALLBACK: Bot cannot answer -> Handover to Admin!
        isHandover = true;
        reply = "🤖 Thắc mắc này nằm ngoài câu trả lời tự động của Bot. Tôi đã chuyển đoạn chat này đến Quản Trị Viên / Thu Ngân trực ban. Nhân viên sẽ hỗ trợ trực tiếp cho bạn ngay tại đây!";
      }

      const botMsgId = 'msg_bot_' + Date.now();
      renderedMsgIds.add(botMsgId);
      chatBody.insertAdjacentHTML('beforeend', `<div class="msg bot">${reply}</div>`);

      if (isHandover) {
        chatBody.insertAdjacentHTML('beforeend', `
          <div class="chat-handover-notice">
            ⚠️ Đã kết nối với Quản Trị Viên • Đang chờ nhân viên trả lời...
          </div>
        `);
        conv.status = 'waiting_admin';
        conv.unread = true;
        conv.waitingReason = `Khách hỏi: "${txt.slice(0, 80)}"`;
      }

      conv.messages.push({
        id: botMsgId,
        sender: 'bot',
        text: reply,
        time: timeStr,
        date: 'Hôm nay',
        isHandover: isHandover
      });

      localStorage.setItem(STORAGE_CHAT_KEY, JSON.stringify(convs));
      chatBody.scrollTop = chatBody.scrollHeight;
    }, 600);
  });
}

function renderMenuPage() {
  const list = document.querySelector("#fullMenuList");
  const filterBtns = document.querySelectorAll("#menuFilter button");
  const cartItems = document.querySelector("#cartItems");
  const cartTotal = document.querySelector("#cartTotal");
  const cartCount = document.querySelector("#cartCount");
  const btnCheckout = document.querySelector("#btnCheckout");
  let cart = {}; // { id: qty }
  let currentFilter = 'all';

  function drawMenu() {
    const items = DATA.menu.filter(m => currentFilter === 'all' || m.category === currentFilter);
    list.innerHTML = items.map(m => `
      <div style="background:#1a1e24;border:1px solid var(--line);border-radius:12px;overflow:hidden;display:flex;flex-direction:column;transition:0.3s" onmouseover="this.style.borderColor='var(--blue)'" onmouseout="this.style.borderColor='var(--line)'">
        <img src="${m.image}" style="width:100%;height:160px;object-fit:cover">
        <div style="padding:15px;display:flex;flex-direction:column;flex:1">
          <h3 style="font-size:16px;margin-bottom:8px">${m.name}</h3>
          <div style="color:var(--blue);font-weight:700;margin-bottom:15px;font-size:15px">${m.price.toLocaleString('vi-VN')} VNĐ</div>
          <button class="btn" style="margin-top:auto;padding:8px;font-size:13px;border:1px solid var(--blue);color:var(--blue);background:transparent" onclick="addToCart('${m.id}')" onmouseover="this.style.background='var(--blue)';this.style.color='#fff'" onmouseout="this.style.background='transparent';this.style.color='var(--blue)'">Thêm vào giỏ</button>
        </div>
      </div>
    `).join('');
  }

  filterBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      filterBtns.forEach(b => { b.classList.remove('blue'); b.style.background='#232830'; b.style.color='#fff'; });
      btn.classList.add('blue');
      btn.style.background='';
      currentFilter = btn.dataset.filter;
      drawMenu();
    });
  });

  window.addToCart = function(id) {
    if(!cart[id]) cart[id] = 0;
    cart[id]++;
    updateCart();
  }
  
  window.updateCartQty = function(id, delta) {
    if(cart[id]) {
      cart[id] += delta;
      if(cart[id] <= 0) delete cart[id];
      updateCart();
    }
  }

  function updateCart() {
    let total = 0;
    let count = 0;
    let html = '';
    
    if(Object.keys(cart).length === 0) {
      cartItems.innerHTML = '<div style="color:var(--text-gray);text-align:center;margin:auto">Giỏ hàng trống</div>';
      cartTotal.textContent = '0 VNĐ';
      cartCount.textContent = '0';
      return;
    }
    
    Object.keys(cart).forEach(id => {
      const item = DATA.menu.find(m => m.id === id);
      const qty = cart[id];
      total += item.price * qty;
      count += qty;
      
      html += `
        <div style="display:flex;justify-content:space-between;align-items:center;background:#232830;padding:12px;border-radius:8px;border:1px solid var(--line)">
          <div style="flex:1">
            <div style="font-weight:600;font-size:14px;margin-bottom:4px">${item.name}</div>
            <div style="color:var(--blue);font-size:13px">${item.price.toLocaleString('vi-VN')}đ</div>
          </div>
          <div style="display:flex;align-items:center;gap:10px">
            <button onclick="updateCartQty('${id}', -1)" style="width:28px;height:28px;border:none;background:#38404a;color:#fff;border-radius:6px;cursor:pointer;font-size:16px;line-height:0">-</button>
            <span style="font-size:14px;font-weight:700;width:24px;text-align:center">${qty}</span>
            <button onclick="updateCartQty('${id}', 1)" style="width:28px;height:28px;border:none;background:#38404a;color:#fff;border-radius:6px;cursor:pointer;font-size:16px;line-height:0">+</button>
          </div>
        </div>
      `;
    });
    
    cartItems.innerHTML = html;
    cartTotal.textContent = total.toLocaleString('vi-VN') + ' VNĐ';
    cartCount.textContent = count;
  }

  if (btnCheckout) {
    btnCheckout.addEventListener('click', () => {
      if(Object.keys(cart).length === 0) {
        alert("Giỏ hàng của bạn đang trống!");
        return;
      }
      const seat = document.querySelector("#cartSeat").value.trim();
      if(!seat) {
        alert("Vui lòng nhập vị trí máy bạn đang ngồi để nhân viên mang đồ ăn ra!");
        document.querySelector("#cartSeat").focus();
        return;
      }
      
      function showSuccess() {
        const modal = document.querySelector("#orderSuccessModal");
        if(modal) {
          modal.classList.add("open");
          modal.querySelectorAll("[data-close-success]").forEach(x=>x.addEventListener("click",()=>{
            modal.classList.remove("open");
            cart = {};
            updateCart();
            document.querySelector("#cartSeat").value = '';
          }));
        } else {
          alert("Đặt món thành công! Nhân viên sẽ mang ra máy " + seat + " trong ít phút.");
          cart = {};
          updateCart();
          document.querySelector("#cartSeat").value = '';
        }
      }

      const paymentMethod = document.querySelector("#cartPayment").value;
      if (paymentMethod === 'transfer') {
        const qrModal = document.querySelector("#qrModal");
        if (qrModal) {
          const totalText = document.querySelector("#cartTotal").textContent;
          qrModal.querySelector("#qrTotalAmount").textContent = totalText;
          qrModal.classList.add("open");
          
          qrModal.querySelector("#btnConfirmPayment").onclick = () => {
            qrModal.classList.remove("open");
            showSuccess();
          };
          qrModal.querySelectorAll("[data-close-qr]").forEach(btn => {
            btn.onclick = () => qrModal.classList.remove("open");
          });
        } else {
          showSuccess();
        }
      } else {
        showSuccess();
      }
    });
  }

  drawMenu();
}
