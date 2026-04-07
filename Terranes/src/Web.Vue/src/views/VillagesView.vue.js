/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/template-helpers.d.ts" />
/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/props-fallback.d.ts" />
import { ref, onMounted, watch } from 'vue';
import { api } from '../api/client';
import LoadingSpinner from '../components/LoadingSpinner.vue';
import DetailModal from '../components/DetailModal.vue';
import StatusBadge from '../components/StatusBadge.vue';
const villages = ref(null);
const searchName = ref('');
const selectedLayout = ref('');
const selectedVillage = ref(null);
const villageLots = ref(null);
const layouts = ['Grid', 'Radial', 'Linear', 'Cluster', 'Freeform'];
async function search() {
    villages.value = await api.getVillages({
        name: searchName.value || undefined,
        layout: selectedLayout.value || undefined,
    });
}
async function viewVillage(village) {
    selectedVillage.value = village;
    villageLots.value = await api.getVillageLots(village.id);
}
function closeModal() {
    selectedVillage.value = null;
    villageLots.value = null;
}
onMounted(search);
watch([searchName, selectedLayout], search);
const __VLS_ctx = {
    ...{},
    ...{},
};
let __VLS_components;
let __VLS_intrinsics;
let __VLS_directives;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "container" },
});
/** @type {__VLS_StyleScopedClasses['container']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.h2, __VLS_intrinsics.h2)({
    ...{ class: "mb-4" },
});
/** @type {__VLS_StyleScopedClasses['mb-4']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
    ...{ class: "text-muted" },
});
/** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "row mb-3" },
});
/** @type {__VLS_StyleScopedClasses['row']} */ ;
/** @type {__VLS_StyleScopedClasses['mb-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-4" },
});
/** @type {__VLS_StyleScopedClasses['col-md-4']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.input)({
    type: "text",
    ...{ class: "form-control" },
    placeholder: "Search by name...",
    value: (__VLS_ctx.searchName),
});
/** @type {__VLS_StyleScopedClasses['form-control']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-3" },
});
/** @type {__VLS_StyleScopedClasses['col-md-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.select, __VLS_intrinsics.select)({
    ...{ class: "form-select" },
    value: (__VLS_ctx.selectedLayout),
});
/** @type {__VLS_StyleScopedClasses['form-select']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.option, __VLS_intrinsics.option)({
    value: "",
});
for (const [layout] of __VLS_vFor((__VLS_ctx.layouts))) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.option, __VLS_intrinsics.option)({
        key: (layout),
        value: (layout),
    });
    (layout);
    // @ts-ignore
    [searchName, selectedLayout, layouts,];
}
if (__VLS_ctx.villages === null) {
    const __VLS_0 = LoadingSpinner;
    // @ts-ignore
    const __VLS_1 = __VLS_asFunctionalComponent1(__VLS_0, new __VLS_0({
        message: "Loading villages...",
    }));
    const __VLS_2 = __VLS_1({
        message: "Loading villages...",
    }, ...__VLS_functionalComponentArgsRest(__VLS_1));
}
else if (__VLS_ctx.villages.length === 0) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "alert alert-info" },
    });
    /** @type {__VLS_StyleScopedClasses['alert']} */ ;
    /** @type {__VLS_StyleScopedClasses['alert-info']} */ ;
}
else {
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "row g-4" },
    });
    /** @type {__VLS_StyleScopedClasses['row']} */ ;
    /** @type {__VLS_StyleScopedClasses['g-4']} */ ;
    for (const [village] of __VLS_vFor((__VLS_ctx.villages))) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "col-md-4" },
            key: (village.id),
        });
        /** @type {__VLS_StyleScopedClasses['col-md-4']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card h-100 shadow-sm" },
        });
        /** @type {__VLS_StyleScopedClasses['card']} */ ;
        /** @type {__VLS_StyleScopedClasses['h-100']} */ ;
        /** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card-body" },
        });
        /** @type {__VLS_StyleScopedClasses['card-body']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.h5, __VLS_intrinsics.h5)({
            ...{ class: "card-title" },
        });
        /** @type {__VLS_StyleScopedClasses['card-title']} */ ;
        (village.name);
        __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
            ...{ class: "card-text text-muted" },
        });
        /** @type {__VLS_StyleScopedClasses['card-text']} */ ;
        /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
        (village.description);
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "d-flex justify-content-between mb-2" },
        });
        /** @type {__VLS_StyleScopedClasses['d-flex']} */ ;
        /** @type {__VLS_StyleScopedClasses['justify-content-between']} */ ;
        /** @type {__VLS_StyleScopedClasses['mb-2']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
            ...{ class: "badge bg-primary" },
        });
        /** @type {__VLS_StyleScopedClasses['badge']} */ ;
        /** @type {__VLS_StyleScopedClasses['bg-primary']} */ ;
        (village.layoutType);
        __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
            ...{ class: "badge bg-secondary" },
        });
        /** @type {__VLS_StyleScopedClasses['badge']} */ ;
        /** @type {__VLS_StyleScopedClasses['bg-secondary']} */ ;
        (village.maxLots);
        __VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({
            ...{ class: "text-muted" },
        });
        /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
        (village.centreLatitude.toFixed(4));
        (village.centreLongitude.toFixed(4));
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card-footer" },
        });
        /** @type {__VLS_StyleScopedClasses['card-footer']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
            ...{ onClick: (...[$event]) => {
                    if (!!(__VLS_ctx.villages === null))
                        return;
                    if (!!(__VLS_ctx.villages.length === 0))
                        return;
                    __VLS_ctx.viewVillage(village);
                    // @ts-ignore
                    [villages, villages, villages, viewVillage,];
                } },
            ...{ class: "btn btn-sm btn-outline-primary" },
        });
        /** @type {__VLS_StyleScopedClasses['btn']} */ ;
        /** @type {__VLS_StyleScopedClasses['btn-sm']} */ ;
        /** @type {__VLS_StyleScopedClasses['btn-outline-primary']} */ ;
        // @ts-ignore
        [];
    }
}
const __VLS_5 = DetailModal || DetailModal;
// @ts-ignore
const __VLS_6 = __VLS_asFunctionalComponent1(__VLS_5, new __VLS_5({
    ...{ 'onClose': {} },
    show: (!!__VLS_ctx.selectedVillage),
    title: (__VLS_ctx.selectedVillage?.name ?? ''),
}));
const __VLS_7 = __VLS_6({
    ...{ 'onClose': {} },
    show: (!!__VLS_ctx.selectedVillage),
    title: (__VLS_ctx.selectedVillage?.name ?? ''),
}, ...__VLS_functionalComponentArgsRest(__VLS_6));
let __VLS_10;
const __VLS_11 = ({ close: {} },
    { onClose: (__VLS_ctx.closeModal) });
const { default: __VLS_12 } = __VLS_8.slots;
if (__VLS_ctx.selectedVillage) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({});
    (__VLS_ctx.selectedVillage.description);
    __VLS_asFunctionalElement1(__VLS_intrinsics.table, __VLS_intrinsics.table)({
        ...{ class: "table table-sm" },
    });
    /** @type {__VLS_StyleScopedClasses['table']} */ ;
    /** @type {__VLS_StyleScopedClasses['table-sm']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.tbody, __VLS_intrinsics.tbody)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedVillage.layoutType);
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedVillage.maxLots);
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedVillage.centreLatitude.toFixed(4));
    (__VLS_ctx.selectedVillage.centreLongitude.toFixed(4));
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (new Date(__VLS_ctx.selectedVillage.createdUtc).toLocaleString());
    if (__VLS_ctx.villageLots && __VLS_ctx.villageLots.length > 0) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.h6, __VLS_intrinsics.h6)({});
        (__VLS_ctx.villageLots.length);
        __VLS_asFunctionalElement1(__VLS_intrinsics.table, __VLS_intrinsics.table)({
            ...{ class: "table table-sm table-striped" },
        });
        /** @type {__VLS_StyleScopedClasses['table']} */ ;
        /** @type {__VLS_StyleScopedClasses['table-sm']} */ ;
        /** @type {__VLS_StyleScopedClasses['table-striped']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.thead, __VLS_intrinsics.thead)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.tbody, __VLS_intrinsics.tbody)({});
        for (const [lot] of __VLS_vFor((__VLS_ctx.villageLots))) {
            __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({
                key: (lot.id),
            });
            __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
            (lot.lotNumber);
            __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
            const __VLS_13 = StatusBadge;
            // @ts-ignore
            const __VLS_14 = __VLS_asFunctionalComponent1(__VLS_13, new __VLS_13({
                status: (lot.status),
            }));
            const __VLS_15 = __VLS_14({
                status: (lot.status),
            }, ...__VLS_functionalComponentArgsRest(__VLS_14));
            __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
            (lot.positionX.toFixed(1));
            (lot.positionY.toFixed(1));
            // @ts-ignore
            [selectedVillage, selectedVillage, selectedVillage, selectedVillage, selectedVillage, selectedVillage, selectedVillage, selectedVillage, selectedVillage, closeModal, villageLots, villageLots, villageLots, villageLots,];
        }
    }
}
// @ts-ignore
[];
var __VLS_8;
var __VLS_9;
// @ts-ignore
[];
const __VLS_export = (await import('vue')).defineComponent({});
export default {};
