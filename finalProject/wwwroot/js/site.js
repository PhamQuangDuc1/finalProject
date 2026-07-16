// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll(".studymate-marquee").forEach((marquee) => {
    marquee.addEventListener("pointerenter", () => marquee.classList.add("is-paused"));
    marquee.addEventListener("pointerleave", () => marquee.classList.remove("is-paused"));
    marquee.addEventListener("focusin", () => marquee.classList.add("is-paused"));
    marquee.addEventListener("focusout", () => marquee.classList.remove("is-paused"));
  });
});
