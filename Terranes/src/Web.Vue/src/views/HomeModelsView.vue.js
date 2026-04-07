/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/template-helpers.d.ts" />
/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/props-fallback.d.ts" />
import { ref, onMounted, watch } from 'vue';
import { api } from '../api/client';
import LoadingSpinner from '../components/LoadingSpinner.vue';
import DetailModal from '../components/DetailModal.vue';
const models = ref(null);
const minBedrooms = ref(undefined);
const selectedFormat = ref('');
const selectedModel = ref(null);
const formats = ['Gltf', 'Glb', 'Obj', 'Fbx', 'Usd'];
async function search() {
    models.value = await api.getHomeModels({
        minBedrooms: minBedrooms.value,
        format: selectedFormat.value || undefined,
    });
}
function selectModel(model) {
    selectedModel.value = model;
}
function closeModal() {
    selectedModel.value = null;
}
onMounted(search);
watch([minBedrooms, selectedFormat], search);
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
    ...{ class: "col-md-3" },
});
/** @type {__VLS_StyleScopedClasses['col-md-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.label, __VLS_intrinsics.label)({
    ...{ class: "form-label" },
});
/** @type {__VLS_StyleScopedClasses['form-label']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.input)({
    type: "number",
    ...{ class: "form-control" },
    min: "0",
    max: "10",
});
(__VLS_ctx.minBedrooms);
/** @type {__VLS_StyleScopedClasses['form-control']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-3" },
});
/** @type {__VLS_StyleScopedClasses['col-md-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.label, __VLS_intrinsics.label)({
    ...{ class: "form-label" },
});
/** @type {__VLS_StyleScopedClasses['form-label']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.select, __VLS_intrinsics.select)({
    ...{ class: "form-select" },
    value: (__VLS_ctx.selectedFormat),
});
/** @type {__VLS_StyleScopedClasses['form-select']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.option, __VLS_intrinsics.option)({
    value: "",
});
for (const [fmt] of __VLS_vFor((__VLS_ctx.formats))) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.option, __VLS_intrinsics.option)({
        key: (fmt),
        value: (fmt),
    });
    (fmt);
    // @ts-ignore
    [minBedrooms, selectedFormat, formats,];
}
if (__VLS_ctx.models === null) {
    const __VLS_0 = LoadingSpinner;
    // @ts-ignore
    const __VLS_1 = __VLS_asFunctionalComponent1(__VLS_0, new __VLS_0({
        message: "Loading home designs...",
    }));
    const __VLS_2 = __VLS_1({
        message: "Loading home designs...",
    }, ...__VLS_functionalComponentArgsRest(__VLS_1));
}
else if (__VLS_ctx.models.length === 0) {
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
    for (const [model] of __VLS_vFor((__VLS_ctx.models))) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "col-md-4" },
            key: (model.id),
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
        (model.name);
        __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
            ...{ class: "card-text text-muted small" },
        });
        /** @type {__VLS_StyleScopedClasses['card-text']} */ ;
        /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
        /** @type {__VLS_StyleScopedClasses['small']} */ ;
        (model.description);
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "row text-center mb-2" },
        });
        /** @type {__VLS_StyleScopedClasses['row']} */ ;
        /** @type {__VLS_StyleScopedClasses['text-center']} */ ;
        /** @type {__VLS_StyleScopedClasses['mb-2']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "col" },
        });
        /** @type {__VLS_StyleScopedClasses['col']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.strong, __VLS_intrinsics.strong)({});
        (model.bedrooms);
        __VLS_asFunctionalElement1(__VLS_intrinsics.br)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "col" },
        });
        /** @type {__VLS_StyleScopedClasses['col']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.strong, __VLS_intrinsics.strong)({});
        (model.bathrooms);
        __VLS_asFunctionalElement1(__VLS_intrinsics.br)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "col" },
        });
        /** @type {__VLS_StyleScopedClasses['col']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.strong, __VLS_intrinsics.strong)({});
        (model.garageSpaces);
        __VLS_asFunctionalElement1(__VLS_intrinsics.br)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "d-flex justify-content-between" },
        });
        /** @type {__VLS_StyleScopedClasses['d-flex']} */ ;
        /** @type {__VLS_StyleScopedClasses['justify-content-between']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
            ...{ class: "badge bg-info" },
        });
        /** @type {__VLS_StyleScopedClasses['badge']} */ ;
        /** @type {__VLS_StyleScopedClasses['bg-info']} */ ;
        (model.format);
        __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({});
        (model.floorAreaSqm.toFixed(0));
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card-footer" },
        });
        /** @type {__VLS_StyleScopedClasses['card-footer']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
            ...{ onClick: (...[$event]) => {
                    if (!!(__VLS_ctx.models === null))
                        return;
                    if (!!(__VLS_ctx.models.length === 0))
                        return;
                    __VLS_ctx.selectModel(model);
                    // @ts-ignore
                    [models, models, models, selectModel,];
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
    show: (!!__VLS_ctx.selectedModel),
    title: (__VLS_ctx.selectedModel?.name ?? ''),
}));
const __VLS_7 = __VLS_6({
    ...{ 'onClose': {} },
    show: (!!__VLS_ctx.selectedModel),
    title: (__VLS_ctx.selectedModel?.name ?? ''),
}, ...__VLS_functionalComponentArgsRest(__VLS_6));
let __VLS_10;
const __VLS_11 = ({ close: {} },
    { onClose: (__VLS_ctx.closeModal) });
const { default: __VLS_12 } = __VLS_8.slots;
if (__VLS_ctx.selectedModel) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({});
    (__VLS_ctx.selectedModel.description);
    __VLS_asFunctionalElement1(__VLS_intrinsics.table, __VLS_intrinsics.table)({
        ...{ class: "table table-sm" },
    });
    /** @type {__VLS_StyleScopedClasses['table']} */ ;
    /** @type {__VLS_StyleScopedClasses['table-sm']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.tbody, __VLS_intrinsics.tbody)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedModel.bedrooms);
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedModel.bathrooms);
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedModel.garageSpaces);
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedModel.floorAreaSqm.toFixed(1));
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedModel.format);
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedModel.fileSizeMb.toFixed(1));
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (new Date(__VLS_ctx.selectedModel.createdUtc).toLocaleString());
}
// @ts-ignore
[selectedModel, selectedModel, selectedModel, selectedModel, selectedModel, selectedModel, selectedModel, selectedModel, selectedModel, selectedModel, selectedModel, closeModal,];
var __VLS_8;
var __VLS_9;
// @ts-ignore
[];
const __VLS_export = (await import('vue')).defineComponent({});
export default {};
