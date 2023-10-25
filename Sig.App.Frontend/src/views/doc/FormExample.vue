<i18n>
{
	"en": {
		"about": "About",
		"about-desc": "Write a few sentences about yourself.",
		"add": "Add",
		"address": "Street address",
		"by-email": "By Email",
		"calendar-more-example-link": "the calendars doc page",
		"calendar-more-example-text": "For more examples of using the calendar in standalone mode, see ",
		"canada": "Canada",
		"candidates": "Candidates",
		"candidates-desc": "Get notified when a candidate applies for a job.",
		"city": "City",
		"comments": "Comments",
		"comments-confirmation": "I confirm that I really want to be notified for any comments posted.",
		"comments-desc": "Get notified when someones posts a comment on a posting.",
		"country": "Country",
		"country-not-accepted-error": "Error during mutation ExampleFormError (The country Impossible Country is automatically refused by the server)",
		"country-placeholder": "Choose your country",
		"date-multi-calendars": "Range date input with two calendars and partial range disallowed",
		"date-range": "Range date input with partial range allowed",
		"date-simple": "Simple date input",
		"datepickers": "Datepickers",
		"email": "Email",
		"email-desc": "Example: you{'@'}example.com",
		"error-summary": "An error prevents sending your form | {count} errors prevent sending your form",
		"everything": "Everything",
		"first-name": "First name",
		"form-complete": "Form sent!",
		"impossible-country": "Impossible Country",
		"last-name": "Last name",
		"loading": "We are processing your form, please wait...",
		"no-push-notifications": "No push notifications",
		"notifications": "Notifications",
		"notifications-desc": "We'll always let you know about important changes, but you pick what else you want to hear about.",
		"offers": "Offers",
		"offers-desc": "Get notified when a candidate accepts or rejects an offer.",
		"password": "Password",
		"password-confirmation": "Password Confirmation",
		"password-rules": "The password must contain a minimum of 10 characters, a capital letter, a number and a special character (for example: %, {'@'}, #, $ et &).",
		"personal-info": "Personal Information",
		"postal-code": "ZIP / Postal code",
		"profile": "Profil",
		"profile-desc": "This information will be displayed publicly so be careful what you share.",
		"province": "State / Province",
		"push-notifications": "Push Notifications",
		"push-notifications-desc": "These are delivered via SMS to your mobile phone.",
		"remove": "Delete",
		"same-as-email": "Same as email",
		"submit": "Submit",
		"subtitle": "ft. PinkFlamant & VeeValidate",
		"title": "Example of complete form",
		"usa": "United States",
		"username": "Username",
		"username-addon": "workcation.com/",
		"username-desc": "Example: your-username",
		"veevalidate-result": "Values returned by VeeValidate",
		"website-name": "Website name",
		"website-url": "Website url",
		"website-url-desc": "Example: https://placeholder.com",
		"websites": "Websites"
	},
	"fr": {
		"about": "À propos",
		"about-desc": "Écrivez quelques phrases à propos de vous.",
		"add": "Ajouter",
		"address": "No et nom de rue",
		"by-email": "Par courriel",
		"calendar-more-example-link": "la page de doc sur les calendriers",
		"calendar-more-example-text": "Pour plus d'exemples d'utilisation du calendrier en mode standalone, voir ",
		"canada": "Canada",
		"candidates": "Candidats",
		"candidates-desc": "Soyez averti quand un candidat postule à un emploi.",
		"city": "Ville",
		"comments": "Commentaires",
		"comments-confirmation": "Je confirme que je veux vraiment être averti pour n'importe quel commentaire publié.",
		"comments-desc": "Soyez averti lorsque quelqu'un publie un commentaire sur une publication.",
		"country": "Pays",
		"country-not-accepted-error": "Erreur lors de la mutation ExampleFormError (Le pays Pays impossible est automatiquement refusé par le serveur)",
		"country-placeholder": "Choisissez votre pays",
		"date-multi-calendars": "Intervalle de dates sur deux calendriers avec intervalle partiel refusé",
		"date-range": "Intervalle de dates avec intervalle partiel accepté",
		"date-simple": "Date unique",
		"datepickers": "Sélecteurs de date",
		"email": "Courriel",
		"email-desc": "Exemple: vous{'@'}exemple.com",
		"error-summary": "Une erreur empêche d'envoyer votre formulaire | {count} erreurs empêchent d'envoyer votre formulaire",
		"everything": "Toutes les notifications",
		"first-name": "Prénom",
		"form-complete": "Formulaire envoyé!",
		"impossible-country": "Pays impossible",
		"last-name": "Nom",
		"loading": "Nous traitons votre formulaire, veuillez patienter...",
		"no-push-notifications": "Aucune notification push",
		"notifications": "Notifications",
		"notifications-desc": "Nous vous informerons toujours des changements importants, mais autrement vous choisissez ce dont vous voulez entendre parler.",
		"offers": "Offres",
		"offers-desc": "Soyez averti quand un candidat accepte ou refuse une offre",
		"password": "Mot de passe",
		"password-confirmation": "Confirmation du mot de passe",
		"password-rules": "Le mot de passe doit contenir un minimum de 10 caractères, une majuscule, un chiffre et un caractère spécial (par exemple: %, {'@'}, #, $ et &).",
		"personal-info": "Informations personnelles",
		"postal-code": "ZIP / Code postal",
		"profile": "Profil",
		"profile-desc": "Ces informations seront affichées publiquement, alors faites attention à ce que vous partagez",
		"province": "État / Province",
		"push-notifications": "Notifications push",
		"push-notifications-desc": "Celles-ci sont envoyées par SMS sur votre téléphone mobile.",
		"remove": "Supprimer",
		"same-as-email": "Seulement celles qui sont envoyés par courriel",
		"submit": "Envoyer",
		"subtitle": "ft. PinkFlamant & VeeValidate",
		"title": "Exemple de formulaire complet",
		"usa": "États-Unis",
		"username": "Nom d'utilisateur",
		"username-addon": "workcation.com/",
		"username-desc": "Exemple: votre-nom-d-utilisateur",
		"veevalidate-result": "Valeurs retournées par VeeValidate",
		"website-name": "Nom du site",
		"website-url": "Url du site",
		"website-url-desc": "Exemple: https://placeholder.com",
		"websites": "Sites web"
	}
}
</i18n>

<template>
  <div class="flex justify-end">
    <ThemeChoice class="mr-4 mt-4" />
  </div>

  <div class="mx-auto max-w-full w-sm px-section my-12 md:my-16">
    <Title class="mb-6" :title="t('title')">
      <template #bottom>
        <span>{{ t("subtitle") }}</span>
      </template>
    </Title>

    <div class="w-sm max-w-full">
      <Form
        v-slot="{ values, errors, isSubmitting }"
        :validation-schema="validationSchema"
        :initial-values="initialValues"
        @submit="onSubmit">
        <PfForm has-footer :submit-label="t('submit')" :loading-label="t('loading')" :processing="isSubmitting">
          <PfFormSection is-grid :title="t('profile')" :description="t('profile-desc')">
            <!-- Nom d'utilisateur -->
            <Field v-slot="{ field, errors: fieldErrors }" name="username">
              <PfFormInputText
                id="username"
                v-bind="field"
                :label="t('username')"
                :errors="fieldErrors"
                :add-on="t('username-addon')"
                :description="t('username-desc')"
                col-span-class="sm:col-span-4" />
            </Field>

            <!-- À propos -->
            <Field v-slot="{ field, errors: fieldErrors }" name="about">
              <PfFormInputTextarea
                id="about"
                v-bind="field"
                :label="t('about')"
                :errors="fieldErrors"
                :description="t('about-desc')"
                col-span-class="sm:col-span-6"
                :rows="4" />
            </Field>
          </PfFormSection>

          <PfFormSection is-grid :title="t('personal-info')">
            <!-- Prénom -->
            <Field v-slot="{ field, errors: fieldErrors }" name="firstName">
              <PfFormInputText
                id="firstName"
                v-bind="field"
                :label="t('first-name')"
                :errors="fieldErrors"
                col-span-class="sm:col-span-3" />
            </Field>

            <!-- Nom -->
            <Field v-slot="{ field, errors: fieldErrors }" name="lastName">
              <PfFormInputText
                id="lastName"
                v-bind="field"
                :label="t('last-name')"
                :errors="fieldErrors"
                col-span-class="sm:col-span-3" />
            </Field>

            <!-- Adresse courriel -->
            <Field v-slot="{ field, errors: fieldErrors }" name="email">
              <PfFormInputText
                id="email"
                v-bind="field"
                :label="t('email')"
                :errors="fieldErrors"
                input-type="email"
                :description="t('email-desc')"
                col-span-class="sm:col-span-4" />
            </Field>

            <!-- Mot de passe -->
            <Field v-slot="{ field, errors: fieldErrors }" name="password">
              <PfFormInputText
                id="password"
                v-bind="field"
                :label="t('password')"
                :errors="fieldErrors"
                input-type="password"
                :description="t('password-rules')"
                col-span-class="sm:col-span-3" />
            </Field>

            <!-- Confirmation du mot de passe -->
            <Field v-slot="{ field, errors: fieldErrors }" name="passwordConfirmation">
              <PfFormInputText
                id="passwordConfirmation"
                v-bind="field"
                :label="t('password-confirmation')"
                :errors="fieldErrors"
                input-type="password"
                col-span-class="sm:col-span-3" />
            </Field>

            <!-- Pays -->
            <Field v-slot="{ field, errors: fieldErrors }" name="country">
              <PfFormInputSelect
                id="country"
                v-bind="field"
                :label="t('country')"
                :options="countryOptions"
                :errors="fieldErrors"
                :placeholder="t('country-placeholder')"
                autocomplete="country-name"
                col-span-class="sm:col-span-3" />
            </Field>

            <!-- Adresse -->
            <Field v-slot="{ field, errors: fieldErrors }" name="address">
              <PfFormInputText
                id="address"
                v-bind="field"
                :label="t('address')"
                :errors="fieldErrors"
                col-span-class="sm:col-span-6" />
            </Field>

            <!-- Ville -->
            <Field v-slot="{ field, errors: fieldErrors }" name="city">
              <PfFormInputText id="city" v-bind="field" :label="t('city')" :errors="fieldErrors" col-span-class="sm:col-span-2" />
            </Field>

            <!-- Province -->
            <Field v-slot="{ field, errors: fieldErrors }" name="province">
              <PfFormInputText
                id="province"
                v-bind="field"
                :label="t('province')"
                :errors="fieldErrors"
                col-span-class="sm:col-span-2" />
            </Field>

            <!-- Code postal -->
            <Field v-slot="{ field, errors: fieldErrors }" name="postalCode">
              <PfFormInputText
                id="postal-code"
                v-bind="field"
                :label="t('postal-code')"
                :errors="fieldErrors"
                col-span-class="sm:col-span-2" />
            </Field>
          </PfFormSection>

          <PfFormSection :title="t('notifications')" :description="t('notifications-desc')">
            <!-- Notifications par courriel -->
            <PfFormFieldset :legend="t('by-email')">
              <Field v-slot="{ field, errors: fieldErrors }" name="comments">
                <PfFormInputCheckbox
                  id="comments"
                  v-bind="field"
                  :label="t('comments')"
                  :description="t('comments-desc')"
                  :errors="fieldErrors"
                  @change="updateCommentsCheckbox" />
              </Field>

              <div v-if="commentsNotifEnabled" class="ml-7">
                <Field v-slot="{ field, errors: fieldErrors }" name="commentsConfirmation">
                  <PfFormInputCheckbox
                    id="comments-confirmation"
                    v-bind="field"
                    :label="t('comments-confirmation')"
                    :errors="fieldErrors" />
                </Field>
              </div>

              <Field v-slot="{ field, errors: fieldErrors }" name="candidates">
                <PfFormInputCheckbox
                  id="candidates"
                  v-bind="field"
                  :label="t('candidates')"
                  :description="t('candidates-desc')"
                  :errors="fieldErrors" />
              </Field>

              <Field v-slot="{ field, errors: fieldErrors }" name="offers">
                <PfFormInputCheckbox
                  id="offers"
                  v-bind="field"
                  :label="t('offers')"
                  :description="t('offers-desc')"
                  :errors="fieldErrors" />
              </Field>
            </PfFormFieldset>

            <!-- Push notifications -->
            <Field v-slot="{ field, errors: fieldErrors }" name="pushNotifications">
              <PfFormInputRadioGroup
                id="push-notifications"
                v-bind="field"
                :label="t('push-notifications')"
                :description="t('push-notifications-desc')"
                :options="pushNotificationsOptions"
                :errors="fieldErrors" />
            </Field>
          </PfFormSection>

          <PfFormSection :title="t('websites')">
            <FieldArray v-slot="{ fields, remove, push }" key-path="id" name="websites">
              <div v-for="(field, idx) in fields" :key="field.key" class="xs:flex xs:items-start xs:gap-x-4">
                <Field v-slot="{ field: inputField, errors: fieldErrors }" :name="`websites[${idx}].url`">
                  <PfFormInputText
                    :id="`websites[${idx}].url`"
                    class="grow"
                    v-bind="inputField"
                    :label="t('website-url')"
                    :description="t('website-url-desc')"
                    :errors="fieldErrors"
                    col-span-class="col-span-3" />
                </Field>
                <Field v-slot="{ field: inputField, errors: fieldErrors }" :name="`websites[${idx}].name`">
                  <PfFormInputText
                    :id="`websites[${idx}].name`"
                    class="grow mt-3 xs:mt-0"
                    v-bind="inputField"
                    :label="t('website-name')"
                    :errors="fieldErrors"
                    col-span-class="col-span-3" />
                </Field>
                <PfButtonAction btn-style="outline" class="mt-3 xs:mt-6" :label="t('remove')" @click="remove(idx)" />
              </div>
              <PfButtonAction type="button" :label="t('add')" @click="push({ id: fields.length, url: '', name: '' })" />
            </FieldArray>
          </PfFormSection>

          <PfFormSection :title="t('datepickers')">
            <Field v-slot="{ field: inputField, errors: fieldErrors }" name="datepicker">
              <DatePicker id="dateSimple" v-bind="inputField" :label="t('date-simple')" :errors="fieldErrors" />
            </Field>
            <Field v-slot="{ field: inputField, errors: fieldErrors }" name="datepickerRange">
              <DatePicker id="dateRange" v-bind="inputField" :label="t('date-range')" :errors="fieldErrors" range partial-range />
            </Field>
            <Field v-slot="{ field: inputField, errors: fieldErrors }" name="datepickerMultiCalendars">
              <DatePicker
                id="dateMultiCalendars"
                v-bind="inputField"
                :label="t('date-multi-calendars')"
                :errors="fieldErrors"
                range
                multi-calendars />
            </Field>
            <p class="text-p3">
              {{ t("calendar-more-example-text")
              }}<RouterLink class="pf-button pf-button--link" :to="{ name: URL_DOC_CALENDAR }">{{
                t("calendar-more-example-link")
              }}</RouterLink>
            </p>
          </PfFormSection>

          <!-- Affichage conditionnel de la liste des erreurs -->
          <PfFormSection v-if="errors && Object.keys(errors).length > 0">
            <div class="rounded-md bg-red-50 dark:bg-grey-800 border-l-4 border-red-400 dark:border-red-600 p-4">
              <div class="flex">
                <div class="shrink-0 h-5 w-5 flex items-center justify-center">
                  <PfIcon class="text-red-400 dark:text-red-500" size="md" :icon="EXCLAMATION_ICON" aria-hidden="true" />
                </div>
                <div class="ml-3">
                  <h3 class="text-sm font-semibold text-red-800 dark:text-red-400 my-0">
                    {{ t("error-summary", Object.keys(errors).length) }}
                  </h3>
                  <div class="mt-2 text-sm text-red-700 dark:text-red-400">
                    <ul role="list" class="mb-0 list-disc pl-5 space-y-1">
                      <li v-for="(error, index) in Object.values(errors)" :key="index">{{ error }}</li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>
          </PfFormSection>

          <PfFormSection>
            <h4 class="my-0">{{ t("veevalidate-result") }}</h4>
            <pre>{{ values }}</pre>
          </PfFormSection>
        </PfForm>
      </Form>
    </div>
  </div>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { ref, onMounted, computed } from "vue";
import { string, object, array, date, ref as yupRef } from "yup";
import gql from "graphql-tag";
import { useMutation } from "@vue/apollo-composable";
import { FieldArray } from "vee-validate";

import DatePicker from "@/components/ui/date-picker.vue";
import Title from "@/components/app/title.vue";
import ThemeChoice from "@/components/accessibility/theme-choice.vue";

import EXCLAMATION_ICON from "@/lib/icons/exclamation-circle-fill.json";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";
import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_DOC_CALENDAR } from "@/lib/consts/urls";
import { usePageTitle } from "@/lib/helpers/page-title";

const { addSuccess } = useNotificationsStore();
const { t } = useI18n();

useGraphQLErrorMessages({
  COUNTRY_NOT_ACCEPTED: () => {
    return t("country-not-accepted-error");
  }
});

const { mutate: exampleFormError } = useMutation(
  gql`
    mutation ExampleFormError($input: ExampleFormErrorInput!) {
      exampleFormError(input: $input) {
        success
      }
    }
  `
);
usePageTitle(t("title"));

// Simulation de l'état de chargement
let loading = ref(true);
onMounted(() => {
  setTimeout(() => {
    loading.value = false;
  }, 200);
});

// Options du champ select de pays
const countryOptions = [
  {
    label: t("canada"),
    value: "canada"
  },
  {
    label: t("usa"),
    value: "usa"
  },
  {
    label: t("impossible-country"),
    value: "impossible-country"
  }
];

// Options du radio group des push notifications
const pushNotificationsOptions = [
  {
    id: "push-everything",
    value: "everything",
    label: t("everything")
  },
  {
    id: "push-email",
    value: "email",
    label: t("same-as-email")
  },
  {
    id: "push-nothing",
    value: "nothing",
    label: t("no-push-notifications")
  }
];

// Gestion de la visibilité et des règles de validation du champ caché
const commentsNotifEnabled = ref(false);
const updateCommentsCheckbox = (e) => {
  commentsNotifEnabled.value = e.target.value === "true" ? true : false;
  validationSchema.value.commentsConfirmation = e.target.value === "true" ? string().label(t("first-name")).required() : null;
};

// Valeurs initiales du formulaire
const initialValues = {
  websites: [
    {
      id: 1,
      url: "",
      name: ""
    }
  ],
  datepicker: new Date("September 22, 2022 15:00:00")
};

// Configuration des règles de validation
const validationSchema = computed(() =>
  object({
    username: string().label(t("username")).required(),
    about: string().label(t("about")).required(),
    email: string().label(t("email")).required().email(),
    firstName: string().label(t("first-name")).required(),
    lastName: string().label(t("last-name")).required(),
    password: string().label(t("password")).required().password(),
    passwordConfirmation: string().label(t("password-confirmation")).required().samePassword(yupRef("password")),
    country: string().label(t("country")).required(),
    address: string().label(t("address")).required(),
    city: string().label(t("city")).required(),
    province: string().label(t("province")).required(),
    postalCode: string().label(t("postal-code")).required(),
    pushNotifications: string().label(t("notifications")).required(),
    commentsConfirmation: commentsNotifEnabled.value ? string().label(t("comments")).required() : null,
    websites: array()
      .of(
        object({
          url: string().label(t("website-url")).required().url(),
          name: string().label(t("website-name")).required()
        })
      )
      .label(t("websites"))
      .required()
      .min(1),
    datepicker: date().label(t("date-simple")).required(),
    datepickerRange: array()
      .of(date().label(t("date-range")).nullable().default(null))
      .label(t("date-range"))
      .required()
      .min(2),
    datepickerMultiCalendars: array()
      .of(date().label(t("date-multi-calendars")).required())
      .label(t("date-multi-calendars"))
      .required()
      .min(2)
  })
);

// Envoi du formulaire si la validation FE a passé
async function onSubmit({ username, about, email, firstName, lastName, password, country, address, city, province, postalCode }) {
  await exampleFormError({
    input: { username, about, email, firstName, lastName, password, country, address, city, province, postalCode }
  });

  addSuccess(t("form-complete"));
}
</script>
