export const commonBtnProps = {
  btnStyle: {
    type: String,
    default: "primary"
  },
  size: {
    type: String,
    default: "md"
  },
  iconSize: {
    type: String,
    default: ""
  },
  label: {
    type: String,
    default: ""
  },
  icon: {
    type: Object,
    default: null
  },
  transition: {
    type: Object,
    default: null
  },
  screenReaderAddon: {
    type: String,
    default: null
  },
  hasIconLeft: Boolean,
  isIconOnly: Boolean,
  isGrouped: Boolean,
  isDisabled: Boolean,
  isDownloadable: Boolean
};
