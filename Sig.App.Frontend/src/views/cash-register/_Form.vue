<i18n>
{
	"en": {
    "project-name": "Program",
    "market-group-name": "Market group",
		"cash-register-name": "Name",
		"cash-register-name-placeholder": "Ex. Grand Market Cash Register",
		"cancel": "Cancel",
    "market-groups": "Program(s)",
    "selected-project": "Program",
    "selected-market-group": "Location",
    "no-associated-market-groups": "All available market groups are associated with the cash register."
	},
	"fr": {
    "project-name": "Programme",
    "market-group-name": "Groupe de commerce",
		"cash-register-name": "Nom",
		"cash-register-name-placeholder": "Ex. Caisse du Grand Marché",
		"cancel": "Annuler",
    "market-groups": "Programme(s)",
    "selected-project": "Programme",
    "selected-market-group": "Lieu",
    "no-associated-market-groups": "Tous les groupes de commerces disponibles sont associés à la caisse."
	}
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, errors: formErrors, setFieldValue }"
    :validation-schema="validationSchema"
    :initial-values="initialValues"
    @submit="onSubmit">
    <PfForm
      has-footer
      can-cancel
      :disable-submit="Object.keys(formErrors).length > 0"
      :submit-label="submitBtn"
      :processing="isSubmitting"
      :cancel-label="t('cancel')"
      :warning-message="warningMessage"
      @cancel="closeModal">
      <PfFormSection>
        <Field v-slot="{ field, errors: fieldErrors }" name="name">
          <PfFormInputText
            id="name"
            required
            v-bind="field"
            :disabled="isAddProject"
            :label="t('cash-register-name')"
            :placeholder="t('cash-register-name-placeholder')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
      </PfFormSection>
      <PfFormSection v-if="!isNew && !isAddProject" :title="t('market-groups')">
        <div class="relative border border-primary-300 rounded-lg px-5 pt-3 pb-6 mb-4 last:mb-0">
          <div v-for="marketGroup in marketGroups" :key="marketGroup.id">
            <div>
              <dt :class="dtClasses">{{ t("project-name") }}</dt>
              <dd :class="ddClasses">{{ marketGroup.project.name }}</dd>
            </div>
            <div>
              <dt :class="dtClasses">{{ t("market-group-name") }}</dt>
              <dd :class="ddClasses">{{ marketGroup.name }}</dd>
            </div>
          </div>
        </div>
      </PfFormSection>
      <template v-else-if="projectOptions.length === 0 || (marketGroupOptions.length === 0 && !isNew && !isAddProject)">
        <div class="text-red-500">
          <p class="text-sm">{{ t("no-associated-market-groups") }}</p>
        </div>
      </template>
      <PfFormSection v-else :title="t('market-groups')">
        <Field v-slot="{ field, errors: fieldErrors }" name="selectedProject">
          <PfFormInputSelect
            id="selectedProject"
            v-bind="field"
            :label="t('selected-project')"
            :options="projectOptions"
            :errors="fieldErrors"
            col-span-class="sm:col-span-3"
            @input="(e) => onProjectSelected(e, setFieldValue)" />
        </Field>
        <Field v-slot="{ field, errors: fieldErrors }" name="selectedMarketGroup">
          <PfFormInputSelect
            id="selectedMarketGroup"
            required
            v-bind="field"
            :label="t('selected-market-group')"
            :options="marketGroupOptions"
            :errors="fieldErrors"
            col-span-class="sm:col-span-3" />
        </Field>
      </PfFormSection>
    </PfForm>
  </Form>
</template>

<script setup>
import { ref, defineEmits, defineProps, computed, watch } from "vue";
import { useI18n } from "vue-i18n";
import { string, object, lazy, mixed } from "yup";

const { t } = useI18n();
const emit = defineEmits(["submit", "closeModal", "nextStep"]);

const dtClasses = "text-primary-500 font-semibold tracking-tight mt-px sm:mt-[3px]";
const ddClasses = "text-primary-900";

const selectedProject = ref(null);

const props = defineProps({
  submitBtn: {
    type: String,
    default: ""
  },
  name: {
    type: String,
    default: ""
  },
  marketGroups: {
    type: Array,
    default() {
      return [];
    }
  },
  market: {
    type: Object,
    default() {
      return {};
    }
  },
  isNew: {
    type: Boolean,
    default: false
  },
  isAddProject: {
    type: Boolean,
    default: false
  }
});

const initialValues = {
  name: props.name,
  selectedProject: null,
  selectedMarketGroup: null
};

const validationSchema = computed(() =>
  object({
    name: string().label(t("cash-register-name")).required(),
    selectedProject: lazy(() => {
      if (!props.isNew)
        return mixed().test({
          test: function () {
            return true;
          }
        });
      return string().label(t("selected-project")).required();
    }),
    selectedMarketGroup: lazy(() => {
      if (!props.isNew)
        return mixed().test({
          test: function () {
            return true;
          }
        });
      return string().label(t("selected-market-group")).required();
    })
  })
);

const projectOptions = computed(() => {
  return (
    props.market?.projects
      ?.filter((x) => !props.marketGroups.some((y) => x.id === y.project.id))
      .map((project) => ({
        value: project.id,
        label: project.name
      })) ?? []
  );
});

const marketGroupOptions = computed(() => {
  var marketGroups = props.market?.projects?.find((project) => project.id === selectedProject.value)?.marketGroups ?? [];
  return marketGroups
    .filter((x) => !props.marketGroups.some((y) => x.id === y.id) && x.markets.find((y) => y.id === props.market.id))
    .map((marketGroup) => {
      return {
        value: marketGroup.id,
        label: marketGroup.name
      };
    });
});

function closeModal() {
  emit("closeModal");
}

async function onSubmit(event) {
  emit("submit", event);
}

function onProjectSelected(e, setFieldValue) {
  selectedProject.value = e;
  if (marketGroupOptions.value.length === 1) {
    setFieldValue("selectedMarketGroup", marketGroupOptions.value[0].value);
  }
}

watch(
  () => props.market,
  () => {
    if (!props.market || !props.market.projects) {
      return;
    }
    if (projectOptions.value.length === 1) {
      initialValues.selectedProject = projectOptions.value[0]?.value;
      selectedProject.value = projectOptions.value[0]?.value;

      if (marketGroupOptions.value.length === 1) {
        initialValues.selectedMarketGroup = marketGroupOptions.value[0].value;
      }
    }
  },
  { immediate: true }
);
</script>
