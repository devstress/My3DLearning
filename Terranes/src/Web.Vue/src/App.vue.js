/// <reference types="../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/template-helpers.d.ts" />
/// <reference types="../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/props-fallback.d.ts" />
import { ref } from 'vue';
import { RouterLink, RouterView } from 'vue-router';
const sidebarOpen = ref(false);
function toggleSidebar() {
    sidebarOpen.value = !sidebarOpen.value;
}
const __VLS_ctx = {
    ...{},
    ...{},
};
let __VLS_components;
let __VLS_intrinsics;
let __VLS_directives;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "page" },
});
/** @type {__VLS_StyleScopedClasses['page']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "sidebar" },
});
/** @type {__VLS_StyleScopedClasses['sidebar']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "top-row ps-3 navbar navbar-dark" },
});
/** @type {__VLS_StyleScopedClasses['top-row']} */ ;
/** @type {__VLS_StyleScopedClasses['ps-3']} */ ;
/** @type {__VLS_StyleScopedClasses['navbar']} */ ;
/** @type {__VLS_StyleScopedClasses['navbar-dark']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "container-fluid" },
});
/** @type {__VLS_StyleScopedClasses['container-fluid']} */ ;
let __VLS_0;
/** @ts-ignore @type {typeof __VLS_components.RouterLink | typeof __VLS_components.RouterLink} */
RouterLink;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent1(__VLS_0, new __VLS_0({
    ...{ class: "navbar-brand" },
    to: "/",
}));
const __VLS_2 = __VLS_1({
    ...{ class: "navbar-brand" },
    to: "/",
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
/** @type {__VLS_StyleScopedClasses['navbar-brand']} */ ;
const { default: __VLS_5 } = __VLS_3.slots;
var __VLS_3;
__VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
    ...{ onClick: (__VLS_ctx.toggleSidebar) },
    ...{ class: "navbar-toggler" },
    type: "button",
});
/** @type {__VLS_StyleScopedClasses['navbar-toggler']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
    ...{ class: "navbar-toggler-icon" },
});
/** @type {__VLS_StyleScopedClasses['navbar-toggler-icon']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ onClick: (...[$event]) => {
            __VLS_ctx.sidebarOpen = false;
            // @ts-ignore
            [toggleSidebar, sidebarOpen,];
        } },
    ...{ class: "nav-scrollable" },
    ...{ class: ({ open: __VLS_ctx.sidebarOpen }) },
});
/** @type {__VLS_StyleScopedClasses['nav-scrollable']} */ ;
/** @type {__VLS_StyleScopedClasses['open']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.nav, __VLS_intrinsics.nav)({
    ...{ class: "nav flex-column" },
});
/** @type {__VLS_StyleScopedClasses['nav']} */ ;
/** @type {__VLS_StyleScopedClasses['flex-column']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "nav-item px-3" },
});
/** @type {__VLS_StyleScopedClasses['nav-item']} */ ;
/** @type {__VLS_StyleScopedClasses['px-3']} */ ;
let __VLS_6;
/** @ts-ignore @type {typeof __VLS_components.RouterLink | typeof __VLS_components.RouterLink} */
RouterLink;
// @ts-ignore
const __VLS_7 = __VLS_asFunctionalComponent1(__VLS_6, new __VLS_6({
    ...{ class: "nav-link" },
    to: "/",
    exactActiveClass: "active",
}));
const __VLS_8 = __VLS_7({
    ...{ class: "nav-link" },
    to: "/",
    exactActiveClass: "active",
}, ...__VLS_functionalComponentArgsRest(__VLS_7));
/** @type {__VLS_StyleScopedClasses['nav-link']} */ ;
const { default: __VLS_11 } = __VLS_9.slots;
__VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
    ...{ class: "bi bi-house-door-fill-nav-menu" },
    'aria-hidden': "true",
});
/** @type {__VLS_StyleScopedClasses['bi']} */ ;
/** @type {__VLS_StyleScopedClasses['bi-house-door-fill-nav-menu']} */ ;
// @ts-ignore
[sidebarOpen,];
var __VLS_9;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "nav-item px-3" },
});
/** @type {__VLS_StyleScopedClasses['nav-item']} */ ;
/** @type {__VLS_StyleScopedClasses['px-3']} */ ;
let __VLS_12;
/** @ts-ignore @type {typeof __VLS_components.RouterLink | typeof __VLS_components.RouterLink} */
RouterLink;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent1(__VLS_12, new __VLS_12({
    ...{ class: "nav-link" },
    to: "/villages",
    activeClass: "active",
}));
const __VLS_14 = __VLS_13({
    ...{ class: "nav-link" },
    to: "/villages",
    activeClass: "active",
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
/** @type {__VLS_StyleScopedClasses['nav-link']} */ ;
const { default: __VLS_17 } = __VLS_15.slots;
__VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
    ...{ class: "bi bi-list-nested-nav-menu" },
    'aria-hidden': "true",
});
/** @type {__VLS_StyleScopedClasses['bi']} */ ;
/** @type {__VLS_StyleScopedClasses['bi-list-nested-nav-menu']} */ ;
// @ts-ignore
[];
var __VLS_15;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "nav-item px-3" },
});
/** @type {__VLS_StyleScopedClasses['nav-item']} */ ;
/** @type {__VLS_StyleScopedClasses['px-3']} */ ;
let __VLS_18;
/** @ts-ignore @type {typeof __VLS_components.RouterLink | typeof __VLS_components.RouterLink} */
RouterLink;
// @ts-ignore
const __VLS_19 = __VLS_asFunctionalComponent1(__VLS_18, new __VLS_18({
    ...{ class: "nav-link" },
    to: "/home-models",
    activeClass: "active",
}));
const __VLS_20 = __VLS_19({
    ...{ class: "nav-link" },
    to: "/home-models",
    activeClass: "active",
}, ...__VLS_functionalComponentArgsRest(__VLS_19));
/** @type {__VLS_StyleScopedClasses['nav-link']} */ ;
const { default: __VLS_23 } = __VLS_21.slots;
__VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
    ...{ class: "bi bi-plus-square-fill-nav-menu" },
    'aria-hidden': "true",
});
/** @type {__VLS_StyleScopedClasses['bi']} */ ;
/** @type {__VLS_StyleScopedClasses['bi-plus-square-fill-nav-menu']} */ ;
// @ts-ignore
[];
var __VLS_21;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "nav-item px-3" },
});
/** @type {__VLS_StyleScopedClasses['nav-item']} */ ;
/** @type {__VLS_StyleScopedClasses['px-3']} */ ;
let __VLS_24;
/** @ts-ignore @type {typeof __VLS_components.RouterLink | typeof __VLS_components.RouterLink} */
RouterLink;
// @ts-ignore
const __VLS_25 = __VLS_asFunctionalComponent1(__VLS_24, new __VLS_24({
    ...{ class: "nav-link" },
    to: "/land",
    activeClass: "active",
}));
const __VLS_26 = __VLS_25({
    ...{ class: "nav-link" },
    to: "/land",
    activeClass: "active",
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
/** @type {__VLS_StyleScopedClasses['nav-link']} */ ;
const { default: __VLS_29 } = __VLS_27.slots;
__VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
    ...{ class: "bi bi-list-nested-nav-menu" },
    'aria-hidden': "true",
});
/** @type {__VLS_StyleScopedClasses['bi']} */ ;
/** @type {__VLS_StyleScopedClasses['bi-list-nested-nav-menu']} */ ;
// @ts-ignore
[];
var __VLS_27;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "nav-item px-3" },
});
/** @type {__VLS_StyleScopedClasses['nav-item']} */ ;
/** @type {__VLS_StyleScopedClasses['px-3']} */ ;
let __VLS_30;
/** @ts-ignore @type {typeof __VLS_components.RouterLink | typeof __VLS_components.RouterLink} */
RouterLink;
// @ts-ignore
const __VLS_31 = __VLS_asFunctionalComponent1(__VLS_30, new __VLS_30({
    ...{ class: "nav-link" },
    to: "/marketplace",
    activeClass: "active",
}));
const __VLS_32 = __VLS_31({
    ...{ class: "nav-link" },
    to: "/marketplace",
    activeClass: "active",
}, ...__VLS_functionalComponentArgsRest(__VLS_31));
/** @type {__VLS_StyleScopedClasses['nav-link']} */ ;
const { default: __VLS_35 } = __VLS_33.slots;
__VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
    ...{ class: "bi bi-list-nested-nav-menu" },
    'aria-hidden': "true",
});
/** @type {__VLS_StyleScopedClasses['bi']} */ ;
/** @type {__VLS_StyleScopedClasses['bi-list-nested-nav-menu']} */ ;
// @ts-ignore
[];
var __VLS_33;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "nav-item px-3" },
});
/** @type {__VLS_StyleScopedClasses['nav-item']} */ ;
/** @type {__VLS_StyleScopedClasses['px-3']} */ ;
let __VLS_36;
/** @ts-ignore @type {typeof __VLS_components.RouterLink | typeof __VLS_components.RouterLink} */
RouterLink;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent1(__VLS_36, new __VLS_36({
    ...{ class: "nav-link" },
    to: "/journey",
    activeClass: "active",
}));
const __VLS_38 = __VLS_37({
    ...{ class: "nav-link" },
    to: "/journey",
    activeClass: "active",
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
/** @type {__VLS_StyleScopedClasses['nav-link']} */ ;
const { default: __VLS_41 } = __VLS_39.slots;
__VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
    ...{ class: "bi bi-plus-square-fill-nav-menu" },
    'aria-hidden': "true",
});
/** @type {__VLS_StyleScopedClasses['bi']} */ ;
/** @type {__VLS_StyleScopedClasses['bi-plus-square-fill-nav-menu']} */ ;
// @ts-ignore
[];
var __VLS_39;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "nav-item px-3" },
});
/** @type {__VLS_StyleScopedClasses['nav-item']} */ ;
/** @type {__VLS_StyleScopedClasses['px-3']} */ ;
let __VLS_42;
/** @ts-ignore @type {typeof __VLS_components.RouterLink | typeof __VLS_components.RouterLink} */
RouterLink;
// @ts-ignore
const __VLS_43 = __VLS_asFunctionalComponent1(__VLS_42, new __VLS_42({
    ...{ class: "nav-link" },
    to: "/dashboard",
    activeClass: "active",
}));
const __VLS_44 = __VLS_43({
    ...{ class: "nav-link" },
    to: "/dashboard",
    activeClass: "active",
}, ...__VLS_functionalComponentArgsRest(__VLS_43));
/** @type {__VLS_StyleScopedClasses['nav-link']} */ ;
const { default: __VLS_47 } = __VLS_45.slots;
__VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
    ...{ class: "bi bi-house-door-fill-nav-menu" },
    'aria-hidden': "true",
});
/** @type {__VLS_StyleScopedClasses['bi']} */ ;
/** @type {__VLS_StyleScopedClasses['bi-house-door-fill-nav-menu']} */ ;
// @ts-ignore
[];
var __VLS_45;
__VLS_asFunctionalElement1(__VLS_intrinsics.main, __VLS_intrinsics.main)({});
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "top-row px-4" },
});
/** @type {__VLS_StyleScopedClasses['top-row']} */ ;
/** @type {__VLS_StyleScopedClasses['px-4']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.a, __VLS_intrinsics.a)({
    href: "https://learn.microsoft.com/aspnet/core/",
    target: "_blank",
});
__VLS_asFunctionalElement1(__VLS_intrinsics.article, __VLS_intrinsics.article)({
    ...{ class: "content px-4" },
});
/** @type {__VLS_StyleScopedClasses['content']} */ ;
/** @type {__VLS_StyleScopedClasses['px-4']} */ ;
let __VLS_48;
/** @ts-ignore @type {typeof __VLS_components.RouterView} */
RouterView;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent1(__VLS_48, new __VLS_48({}));
const __VLS_50 = __VLS_49({}, ...__VLS_functionalComponentArgsRest(__VLS_49));
// @ts-ignore
[];
const __VLS_export = (await import('vue')).defineComponent({});
export default {};
