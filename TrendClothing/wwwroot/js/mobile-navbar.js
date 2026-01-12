const openBtn = document.getElementById("openMobileMenu");
const closeBtn = document.getElementById("closeMobileMenu");
const drawer = document.getElementById("mobileDrawer");
const overlay = document.getElementById("drawerOverlay");

/* Drawer open */
openBtn?.addEventListener("click", () => {
    drawer.classList.add("open");
    overlay.style.display = "block";
});

/* Drawer close */
closeBtn?.addEventListener("click", closeDrawer);
overlay?.addEventListener("click", closeDrawer);

function closeDrawer() {
    drawer.classList.remove("open");
    overlay.style.display = "none";
}

/* ================= DROPDOWN LOGIC ================= */
document.querySelectorAll(".drawer-toggle").forEach(toggle => {
    toggle.addEventListener("click", () => {
        const parent = toggle.closest(".drawer-dropdown");
        const submenu = parent.querySelector(".drawer-submenu");

        // close other dropdowns
        document.querySelectorAll(".drawer-dropdown").forEach(d => {
            if (d !== parent) {
                d.classList.remove("open");
                d.querySelector(".drawer-submenu").style.display = "none";
            }
        });

        // toggle current
        parent.classList.toggle("open");
        submenu.style.display =
            submenu.style.display === "block" ? "none" : "block";
    });
});
