/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/template-helpers.d.ts" />
/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/props-fallback.d.ts" />
import { computed } from 'vue';
const props = defineProps();
const defaultColors = {
    Active: 'bg-success',
    Draft: 'bg-warning',
    UnderOffer: 'bg-info',
    Sold: 'bg-danger',
    Withdrawn: 'bg-secondary',
    Browsing: 'bg-info',
    DesignSelected: 'bg-primary',
    PlacedOnLand: 'bg-primary',
    Customising: 'bg-warning text-dark',
    QuoteRequested: 'bg-warning text-dark',
    QuoteReceived: 'bg-success',
    Referred: 'bg-success',
    Completed: 'bg-success',
    Abandoned: 'bg-danger',
    Occupied: 'bg-success',
    Vacant: 'bg-warning',
};
const badgeClass = computed(() => {
    const map = props.colorMap ?? defaultColors;
    return map[props.status] ?? 'bg-secondary';
});
const __VLS_ctx = {
    ...{},
    ...{},
    ...{},
    ...{},
};
let __VLS_components;
let __VLS_intrinsics;
let __VLS_directives;
__VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
    ...{ class: "badge" },
    ...{ class: (__VLS_ctx.badgeClass) },
});
/** @type {__VLS_StyleScopedClasses['badge']} */ ;
(__VLS_ctx.status);
// @ts-ignore
[badgeClass, status,];
const __VLS_export = (await import('vue')).defineComponent({
    __typeProps: {},
});
export default {};
