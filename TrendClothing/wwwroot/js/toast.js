function showToast(message, color = "#333") {
    let toast = document.getElementById("toast");
    toast.innerText = message;
    toast.style.background = color;
    toast.classList.add("show");

    setTimeout(() => {
        toast.classList.remove("show");
    }, 3000);
}
