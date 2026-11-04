
document.addEventListener(
    "click",
    (event) => {
        const button = event.target.closest?.("button");

        if (!button) return;

        if (
            button.getAttribute("aria-label") !==
            "Remove authorization"
        ) {
            return;
        }

        if (!button.closest(".swagger-ui .auth-btn-wrapper")) {
            return;
        }

        // Permitir primero que Swagger elimine su autorización.
        // Después redirigir al logout real de Keycloak.
        setTimeout(() => {
            window.location.assign("/api/v1/auth/logout");
        }, 0);
    },
    true
);
