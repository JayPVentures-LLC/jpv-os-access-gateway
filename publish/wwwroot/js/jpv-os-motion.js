(() => {
 const cards = document.querySelectorAll('.jpv-card');

 cards.forEach(card => {
   card.addEventListener('mousemove', e => {
     const rect = card.getBoundingClientRect();
     const x = e.clientX - rect.left;
     const y = e.clientY - rect.top;

     card.style.transform =
      'rotateX(' + (-(y - rect.height/2)/40) + 'deg) rotateY(' + ((x - rect.width/2)/40) + 'deg) translateY(-6px)';
   });

   card.addEventListener('mouseleave', () => {
      card.style.transform = '';
   });
 });
})();
